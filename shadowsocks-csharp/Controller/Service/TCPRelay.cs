using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Timers;

using Shadowsocks.Controller.Strategy;
using Shadowsocks.Encryption;
using Shadowsocks.Model;
using Shadowsocks.Proxy;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    class TCPRelay : Listener.Service
    {
        private ShadowsocksController _controller;
        private DateTime _lastSweepTime;
        private Configuration _config;

        public ISet<TCPHandler> Handlers { get; set; }

        public TCPRelay(ShadowsocksController controller, Configuration conf)
        {
            _controller = controller;
            _config = conf;
            Handlers = new HashSet<TCPHandler>();
            _lastSweepTime = DateTime.Now;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Tcp
                || (length < 2 || firstPacket[0] != 5))
                return false;
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            TCPHandler handler = new TCPHandler(this, _config);
            handler.connection = socket;
            handler.controller = _controller;
            handler.tcprelay = this;

            handler.Start(firstPacket, length);
            IList<TCPHandler> handlersToClose = new List<TCPHandler>();
            lock (Handlers)
            {
                Handlers.Add(handler);
                DateTime now = DateTime.Now;
                if (now - _lastSweepTime > TimeSpan.FromSeconds(1))
                {
                    _lastSweepTime = now;
                    foreach (TCPHandler handler1 in Handlers)
                        if (now - handler1.lastActivity > TimeSpan.FromSeconds(900))
                            handlersToClose.Add(handler1);
                }
            }
            foreach (TCPHandler handler1 in handlersToClose)
            {
                Logging.Debug("Closing timed out TCP connection.");
                handler1.Close();
            }
            return true;
        }

        public void UpdateInboundCounter(Server server, long n)
        {
            _controller.UpdateInboundCounter(server, n);
        }

        public void UpdateOutboundCounter(Server server, long n)
        {
            _controller.UpdateOutboundCounter(server, n);
        }

        public void UpdateLatency(Server server, TimeSpan latency)
        {
            _controller.UpdateLatency(server, latency);
        }
    }

    class TCPHandler
    {
        // Size of receive buffer.
        public static readonly int RecvSize = 8192;
        public static readonly int RecvReserveSize = IVEncryptor.ONETIMEAUTH_BYTES + IVEncryptor.AUTH_BYTES; // reserve for one-time auth
        public static readonly int BufferSize = RecvSize + RecvReserveSize + 32;

        // public Encryptor encryptor;
        public IEncryptor encryptor;
        public Server server;
        // Client  socket.
        public IProxy remote;
        public Socket connection;
        public ShadowsocksController controller;
        public TCPRelay tcprelay;

        public DateTime lastActivity;

        private const int MaxRetry = 4;
        private int _retryCount = 0;
        private bool _proxyConnected;
        private bool _destConnected;

        private byte _command;
        private byte[] _firstPacket;
        private int _firstPacketLength;

        private int _totalRead = 0;
        private int _totalWrite = 0;

        private byte[] _remoteRecvBuffer = new byte[BufferSize];
        private byte[] _remoteSendBuffer = new byte[BufferSize];
        private byte[] _connetionRecvBuffer = new byte[BufferSize];
        private byte[] _connetionSendBuffer = new byte[BufferSize];

        private bool _connectionShutdown = false;
        private bool _remoteShutdown = false;
        private bool _closed = false;

        private object _encryptionLock = new object();
        private object _decryptionLock = new object();

        private DateTime _startConnectTime;
        private DateTime _startReceivingTime;
        private DateTime _startSendingTime;
        private int _bytesToSend;
        private TCPRelay _tcprelay;  // TODO: is _tcprelay equals tcprelay declared above?
        private Configuration _config;

        public TCPHandler(TCPRelay tcprelay, Configuration config)
        {
            this._tcprelay = tcprelay;
            this._config = config;
        }

        public void CreateRemote()
        {
            Server server = controller.GetAServer(IStrategyCallerType.TCP, (IPEndPoint)connection.RemoteEndPoint);
            if (server == null || server.server == "")
                throw new ArgumentException("No server configured");
            encryptor = EncryptorFactory.GetEncryptor(server.method, server.password, server.auth, false);
            this.server = server;
        }

        public void Start(byte[] firstPacket, int length)
        {
            _firstPacket = firstPacket;
            _firstPacketLength = length;
            HandshakeReceive();
            lastActivity = DateTime.Now;
        }

        private void CheckClose()
        {
            if (_connectionShutdown && _remoteShutdown)
                Close();
        }

        public void Close()
        {
            lock (tcprelay.Handlers)
            {
                tcprelay.Handlers.Remove(this);
            }
            lock (this) {
                if (_closed) return;
                _closed = true;
            }
            try
            {
                connection?.Shutdown(SocketShutdown.Both);
                connection?.Close();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
            try
            {
                remote?.Shutdown(SocketShutdown.Both);
                remote?.Close();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
            lock (_encryptionLock)
            {
                lock (_decryptionLock)
                {
                    encryptor?.Dispose();
                }
            }
        }

        private void HandshakeReceive()
        {
            if (_closed) return;
            try
            {
                int bytesRead = _firstPacketLength;
                if (bytesRead > 1)
                {
                    byte[] response = { 5, 0 };
                    if (_firstPacket[0] != 5)
                    {
                        // reject socks 4
                        response = new byte[] { 0, 91 };
                        Logging.Error("socks 5 protocol error");
                    }
                    connection?.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(HandshakeSendCallback), null);
                }
                else
                    Close();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void HandshakeSendCallback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                connection.EndSend(ar);

                // +-----+-----+-------+------+----------+----------+
                // | VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +-----+-----+-------+------+----------+----------+
                // |  1  |  1  | X'00' |  1   | Variable |    2     |
                // +-----+-----+-------+------+----------+----------+
                // Skip first 3 bytes
                // TODO validate
                connection.BeginReceive(_connetionRecvBuffer, 0, 3, SocketFlags.None, new AsyncCallback(handshakeReceive2Callback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void handshakeReceive2Callback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                int bytesRead = connection.EndReceive(ar);
                if (bytesRead >= 3)
                {
                    _command = _connetionRecvBuffer[1];
                    if (_command == 1)
                    {
                        byte[] response = { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
                        connection.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(ResponseCallback), null);
                    }
                    else if (_command == 3)
                        HandleUDPAssociate();
                }
                else
                {
                    Logging.Debug("failed to recv data in Shadowsocks.Controller.TCPHandler.handshakeReceive2Callback()");
                    Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void HandleUDPAssociate()
        {
            IPEndPoint endPoint = (IPEndPoint)connection.LocalEndPoint;
            byte[] address = endPoint.Address.GetAddressBytes();
            int port = endPoint.Port;
            byte[] response = new byte[4 + address.Length + 2];
            response[0] = 5;
            switch (endPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    response[3] = 1;
                    break;
                case AddressFamily.InterNetworkV6:
                    response[3] = 4;
                    break;
            }
            address.CopyTo(response, 4);
            response[response.Length - 1] = (byte)(port & 0xFF);
            response[response.Length - 2] = (byte)((port >> 8) & 0xFF);
            connection.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(ReadAll), true);
        }

        private void ReadAll(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                if (ar.AsyncState != null)
                {
                    connection.EndSend(ar);
                    Logging.Debug(remote.LocalEndPoint, remote.DestEndPoint, RecvSize, "TCP Relay");
                    connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(ReadAll), null);
                }
                else
                {
                    int bytesRead = connection.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        Logging.Debug(remote.LocalEndPoint, remote.DestEndPoint, RecvSize, "TCP Relay");
                        connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(ReadAll), null);
                    }
                    else
                        Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void ResponseCallback(IAsyncResult ar)
        {
            try
            {
                connection?.EndSend(ar);
                StartConnect();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        // inner class
        private class ProxyTimer : Timer
        {
            public IProxy Proxy;
            public EndPoint DestEndPoint;
            public Server Server;

            public ProxyTimer(int p) : base(p)
            {
            }
        }

        private class ServerTimer : Timer
        {
            public Server Server;
            public ServerTimer(int p) : base(p) { }
        }

        private void StartConnect()
        {
            try
            {
                CreateRemote();

                // Setting up proxy
                EndPoint proxyEP;
                if (_config.useProxy)
                {
                    remote = new Socks5Proxy();
                    proxyEP = SocketUtil.GetEndPoint(_config.proxyServer, _config.proxyPort);
                }
                else
                {
                    remote = new DirectConnect();
                    proxyEP = null;
                }


                ProxyTimer proxyTimer = new ProxyTimer(3000);
                proxyTimer.AutoReset = false;
                proxyTimer.Elapsed += proxyConnectTimer_Elapsed;
                proxyTimer.Enabled = true;

                proxyTimer.Proxy = remote;
                proxyTimer.DestEndPoint = SocketUtil.GetEndPoint(server.server, server.server_port);
                proxyTimer.Server = server;

                _proxyConnected = false;

                // Connect to the proxy server.
                remote.BeginConnectProxy(proxyEP, new AsyncCallback(ProxyConnectCallback), proxyTimer);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void proxyConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_proxyConnected || _destConnected)
            {
                return;
            }
            var proxy = ((ProxyTimer)sender).Proxy;

            Logging.Info($"Proxy {proxy.ProxyEndPoint} timed out");
            remote?.Close();
            RetryConnect();
        }

        private void ProxyConnectCallback(IAsyncResult ar)
        {
            Server server = null;
            if (_closed)
            {
                return;
            }
            try
            {
                ProxyTimer timer = (ProxyTimer)ar.AsyncState;
                var destEndPoint = timer.DestEndPoint;
                server = timer.Server;
                timer.Elapsed -= proxyConnectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();

                // Complete the connection.
                remote.EndConnectProxy(ar);

                _proxyConnected = true;

                if (_config.isVerboseLogging)
                {
                    if (!(remote is DirectConnect))
                    {
                        Logging.Info($"Socket connected to proxy {remote.ProxyEndPoint}");
                    }
                }

                _startConnectTime = DateTime.Now;
                ServerTimer connectTimer = new ServerTimer(3000);
                connectTimer.AutoReset = false;
                connectTimer.Elapsed += destConnectTimer_Elapsed;
                connectTimer.Enabled = true;
                connectTimer.Server = server;
                
                _destConnected = false;
                // Connect to the remote endpoint.
                remote.BeginConnectDest(destEndPoint, new AsyncCallback(ConnectCallback), connectTimer);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                RetryConnect();
            }
        }

        private void destConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_destConnected)
            {
                return;
            }

            Server server = ((ServerTimer)sender).Server;
            IStrategy strategy = controller.GetCurrentStrategy();
            strategy?.SetFailure(server);
            Logging.Info($"{server.FriendlyName()} timed out");
            remote?.Close();
            RetryConnect();
        }

        private void RetryConnect()
        {
            if (_retryCount < MaxRetry)
            {
                Logging.Debug($"Connection failed, retry ({_retryCount})");
                StartConnect();
                _retryCount++;
            }
            else
                Close();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                ServerTimer timer = (ServerTimer)ar.AsyncState;
                server = timer.Server;
                timer.Elapsed -= destConnectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();

                // Complete the connection.
                remote?.EndConnectDest(ar);
                
                _destConnected = true;

                if (_config.isVerboseLogging)
                {
                    Logging.Info($"Socket connected to ss server: {server.FriendlyName()}");
                }

                var latency = DateTime.Now - _startConnectTime;
                IStrategy strategy = controller.GetCurrentStrategy();
                strategy?.UpdateLatency(server, latency);
                _tcprelay.UpdateLatency(server, latency);

                StartPipe();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                if (server != null)
                {
                    IStrategy strategy = controller.GetCurrentStrategy();
                    strategy?.SetFailure(server);
                }
                Logging.LogUsefulException(e);
                RetryConnect();
            }
        }

        private void StartPipe()
        {
            if (_closed) return;
            try
            {
                _startReceivingTime = DateTime.Now;
                remote?.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeRemoteReceiveCallback), null);
                connection?.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void PipeRemoteReceiveCallback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                if ( remote == null ) return;
                int bytesRead = remote.EndReceive(ar);
                _totalRead += bytesRead;
                _tcprelay.UpdateInboundCounter(server, bytesRead);
                if (bytesRead > 0)
                {
                    lastActivity = DateTime.Now;
                    int bytesToSend;
                    lock (_decryptionLock)
                    {
                        if (_closed) return;
                        encryptor.Decrypt(_remoteRecvBuffer, bytesRead, _remoteSendBuffer, out bytesToSend);
                    }
                    connection.BeginSend(_remoteSendBuffer, 0, bytesToSend, SocketFlags.None, new AsyncCallback(PipeConnectionSendCallback), null);
                    IStrategy strategy = controller.GetCurrentStrategy();
                    strategy?.UpdateLastRead(server);
                }
                else
                {
                    connection.Shutdown(SocketShutdown.Send);
                    _connectionShutdown = true;
                    CheckClose();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void PipeConnectionReceiveCallback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                if(connection == null) return;
                int bytesRead = connection.EndReceive(ar);
                _totalWrite += bytesRead;
                if (bytesRead > 0)
                {
                    int atyp = _connetionRecvBuffer[0];
                    string dst_addr;
                    int dst_port;
                    switch (atyp)
                    {
                        case 1:  // IPv4 address, 4 bytes
                            dst_addr = new IPAddress(_connetionRecvBuffer.Skip(1).Take(4).ToArray()).ToString();
                            dst_port = (_connetionRecvBuffer[5] << 8) + _connetionRecvBuffer[6];
                            if ( _config.isVerboseLogging ) {
                                Logging.Info( $"connect to {dst_addr}:{dst_port}" );
                            }
                            break;
                        case 3:  // domain name, length + str
                            int len = _connetionRecvBuffer[1];
                            dst_addr = System.Text.Encoding.UTF8.GetString(_connetionRecvBuffer, 2, len);
                            dst_port = (_connetionRecvBuffer[len + 2] << 8) + _connetionRecvBuffer[len + 3];
                            if ( _config.isVerboseLogging ) {
                                Logging.Info( $"connect to {dst_addr}:{dst_port}" );
                            }
                            break;
                        case 4:  // IPv6 address, 16 bytes
                            dst_addr = new IPAddress(_connetionRecvBuffer.Skip(1).Take(16).ToArray()).ToString();
                            dst_port = (_connetionRecvBuffer[17] << 8) + _connetionRecvBuffer[18];
                            if ( _config.isVerboseLogging ) {
                                Logging.Info( $"connect to [{dst_addr}]:{dst_port}" );
                            }
                            break;
                    }
                    int bytesToSend;
                    lock (_encryptionLock)
                    {
                        if (_closed) return;
                        encryptor.Encrypt(_connetionRecvBuffer, bytesRead, _connetionSendBuffer, out bytesToSend);
                    }
                    _tcprelay.UpdateOutboundCounter(server, bytesToSend);
                    _startSendingTime = DateTime.Now;
                    _bytesToSend = bytesToSend;
                    remote.BeginSend(_connetionSendBuffer, 0, bytesToSend, SocketFlags.None, new AsyncCallback(PipeRemoteSendCallback), null);
                    IStrategy strategy = controller.GetCurrentStrategy();
                    strategy?.UpdateLastWrite(server);
                }
                else
                {
                    remote.Shutdown(SocketShutdown.Send);
                    _remoteShutdown = true;
                    CheckClose();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void PipeRemoteSendCallback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                remote?.EndSend(ar);
                connection?.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void PipeConnectionSendCallback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                connection?.EndSend(ar);
                remote?.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeRemoteReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }
    }
}
