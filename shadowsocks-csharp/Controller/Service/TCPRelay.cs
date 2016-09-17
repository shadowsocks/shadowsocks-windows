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
using Shadowsocks.Util.Sockets;

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

        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Tcp
                || (length < 2 || firstPacket[0] != 5))
                return false;
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            TCPHandler handler = new TCPHandler(_controller, _config, this, socket);

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

            /*
             * Start after we put it into Handlers set. Otherwise if it failed in handler.Start()
             * then it will call handler.Close() before we add it into the set.
             * Then the handler will never release until the next Handle call. Sometimes it will
             * cause odd problems (especially during memory profiling).
             */
            handler.Start(firstPacket, length);

            return true;
        }

        public override void Stop()
        {
            List<TCPHandler> handlersToClose = new List<TCPHandler>();
            lock (Handlers)
            {
                handlersToClose.AddRange(Handlers);
            }
            handlersToClose.ForEach(h=>h.Close());
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

        class AsyncSession
        {
            public IProxy Remote { get; }

            public AsyncSession(IProxy remote)
            {
                Remote = remote;
            }
        }

        class AsyncSession<T> : AsyncSession
        {
            public T State { get; set; }

            public AsyncSession(IProxy remote, T state) : base(remote)
            {
                State = state;
            }

            public AsyncSession(AsyncSession session, T state): base(session.Remote)
            {
                State = state;
            }
        }


        // Size of receive buffer.
        public static readonly int RecvSize = 8192;
        public static readonly int RecvReserveSize = IVEncryptor.ONETIMEAUTH_BYTES + IVEncryptor.AUTH_BYTES; // reserve for one-time auth
        public static readonly int BufferSize = RecvSize + RecvReserveSize + 32;

        public DateTime lastActivity;

        private ShadowsocksController   _controller;
        private Configuration           _config;
        private TCPRelay                _tcprelay;
        private Socket                  _connection;

        private IEncryptor  _encryptor;
        private Server      _server;

        private AsyncSession _currentRemoteSession;

        private const int   MaxRetry = 4;
        private int         _retryCount = 0;
        private bool        _proxyConnected;
        private bool        _destConnected;

        private byte    _command;
        private byte[]  _firstPacket;
        private int     _firstPacketLength;

        private int     _totalRead = 0;
        private int     _totalWrite = 0;

        private byte[]  _remoteRecvBuffer = new byte[BufferSize];
        private byte[]  _remoteSendBuffer = new byte[BufferSize];
        private byte[]  _connetionRecvBuffer = new byte[BufferSize];
        private byte[]  _connetionSendBuffer = new byte[BufferSize];

        private bool    _connectionShutdown = false;
        private bool    _remoteShutdown = false;
        private bool    _closed = false;

        private object  _encryptionLock = new object();
        private object  _decryptionLock = new object();

        private DateTime _startConnectTime;
        private DateTime _startReceivingTime;
        private DateTime _startSendingTime;

        public TCPHandler(ShadowsocksController controller, Configuration config, TCPRelay tcprelay, Socket socket)
        {
            this._controller = controller;
            this._config = config;
            this._tcprelay = tcprelay;
            this._connection = socket;

            lastActivity = DateTime.Now;
        }

        public void CreateRemote()
        {
            Server server = _controller.GetAServer(IStrategyCallerType.TCP, (IPEndPoint)_connection.RemoteEndPoint);
            if (server == null || server.server == "")
                throw new ArgumentException("No server configured");
            lock (_encryptionLock)
            {
                lock (_decryptionLock)
                {
                    _encryptor = EncryptorFactory.GetEncryptor(server.method, server.password, server.auth, false);
                }
            }
            this._server = server;
        }

        public void Start(byte[] firstPacket, int length)
        {
            _firstPacket = firstPacket;
            _firstPacketLength = length;
            HandshakeReceive();
        }

        private void CheckClose()
        {
            if (_connectionShutdown && _remoteShutdown)
                Close();
        }

        public void Close()
        {
            lock (this)
            {
                if (_closed) return;
                _closed = true;
            }
            lock (_tcprelay.Handlers)
            {
                _tcprelay.Handlers.Remove(this);
            }
            try
            {
                _connection?.Shutdown(SocketShutdown.Both);
                _connection?.Close();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
            try
            {
                var remote = _currentRemoteSession?.Remote;
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
                    _encryptor?.Dispose();
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
                    _connection?.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(HandshakeSendCallback), null);
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
                _connection.EndSend(ar);

                // +-----+-----+-------+------+----------+----------+
                // | VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +-----+-----+-------+------+----------+----------+
                // |  1  |  1  | X'00' |  1   | Variable |    2     |
                // +-----+-----+-------+------+----------+----------+
                // Skip first 3 bytes
                // TODO validate
                _connection.BeginReceive(_connetionRecvBuffer, 0, 3, SocketFlags.None, new AsyncCallback(handshakeReceive2Callback), null);
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
                int bytesRead = _connection.EndReceive(ar);
                if (bytesRead >= 3)
                {
                    _command = _connetionRecvBuffer[1];
                    if (_command == 1)
                    {
                        byte[] response = { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
                        _connection.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(ResponseCallback), null);
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
            IPEndPoint endPoint = (IPEndPoint)_connection.LocalEndPoint;
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
            _connection.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(ReadAll), true);
        }

        private void ReadAll(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                if (ar.AsyncState != null)
                {
                    _connection.EndSend(ar);
                    _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(ReadAll), null);
                }
                else
                {
                    int bytesRead = _connection.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(ReadAll), null);
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
                _connection?.EndSend(ar);
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
            public AsyncSession Session;

            public EndPoint DestEndPoint;
            public Server Server;

            public ProxyTimer(int p) : base(p)
            {
            }
        }

        private class ServerTimer : Timer
        {
            public AsyncSession Session;

            public Server Server;
            public ServerTimer(int p) : base(p) { }
        }

        private void StartConnect()
        {
            try
            {
                CreateRemote();

                // Setting up proxy
                IProxy remote;
                EndPoint proxyEP;
                if (_config.proxy.useProxy)
                {
                    remote = new Socks5Proxy();
                    proxyEP = SocketUtil.GetEndPoint(_config.proxy.proxyServer, _config.proxy.proxyPort);
                }
                else
                {
                    remote = new DirectConnect();
                    proxyEP = null;
                }

                var session = new AsyncSession(remote);
                _currentRemoteSession = session;

                ProxyTimer proxyTimer = new ProxyTimer(3000);
                proxyTimer.AutoReset = false;
                proxyTimer.Elapsed += proxyConnectTimer_Elapsed;
                proxyTimer.Enabled = true;

                proxyTimer.Session = session;
                proxyTimer.DestEndPoint = SocketUtil.GetEndPoint(_server.server, _server.server_port);
                proxyTimer.Server = _server;

                _proxyConnected = false;

                // Connect to the proxy server.
                remote.BeginConnectProxy(proxyEP, new AsyncCallback(ProxyConnectCallback), new AsyncSession<ProxyTimer>(remote, proxyTimer));
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void proxyConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = (ProxyTimer) sender;
            timer.Elapsed -= proxyConnectTimer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();


            if (_proxyConnected || _destConnected || _closed)
            {
                return;
            }
            var proxy = timer.Session.Remote;

            Logging.Info($"Proxy {proxy.ProxyEndPoint} timed out");
            proxy.Close();
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
                var session = (AsyncSession<ProxyTimer>) ar.AsyncState;
                ProxyTimer timer = session.State;
                var destEndPoint = timer.DestEndPoint;
                server = timer.Server;
                timer.Elapsed -= proxyConnectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();

                var remote = session.Remote;

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
                connectTimer.Session = session;
                connectTimer.Server = server;
                
                _destConnected = false;
                // Connect to the remote endpoint.
                remote.BeginConnectDest(destEndPoint, new AsyncCallback(ConnectCallback), new AsyncSession<ServerTimer>(session, connectTimer));
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
            var timer = (ServerTimer)sender;
            timer.Elapsed -= destConnectTimer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();

            if (_destConnected || _closed)
            {
                return;
            }

            var session = timer.Session;
            Server server = timer.Server;
            IStrategy strategy = _controller.GetCurrentStrategy();
            strategy?.SetFailure(server);
            Logging.Info($"{server.FriendlyName()} timed out");
            session.Remote.Close();
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
                var session = (AsyncSession<ServerTimer>) ar.AsyncState;
                ServerTimer timer = session.State;
                _server = timer.Server;
                timer.Elapsed -= destConnectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();

                var remote = session.Remote;
                // Complete the connection.
                remote?.EndConnectDest(ar);
                
                _destConnected = true;

                if (_config.isVerboseLogging)
                {
                    Logging.Info($"Socket connected to ss server: {_server.FriendlyName()}");
                }

                var latency = DateTime.Now - _startConnectTime;
                IStrategy strategy = _controller.GetCurrentStrategy();
                strategy?.UpdateLatency(_server, latency);
                _tcprelay.UpdateLatency(_server, latency);

                StartPipe(session);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                if (_server != null)
                {
                    IStrategy strategy = _controller.GetCurrentStrategy();
                    strategy?.SetFailure(_server);
                }
                Logging.LogUsefulException(e);
                RetryConnect();
            }
        }

        private void StartPipe(AsyncSession session)
        {
            if (_closed) return;
            try
            {
                _startReceivingTime = DateTime.Now;
                session.Remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeRemoteReceiveCallback), session);
                _connection?.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeConnectionReceiveCallback), 
                    new AsyncSession<bool>(session, true) /* to tell the callback this is the first time reading packet, and we haven't found the header yet. */);
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
                var session = (AsyncSession) ar.AsyncState;
                int bytesRead = session.Remote.EndReceive(ar);
                _totalRead += bytesRead;
                _tcprelay.UpdateInboundCounter(_server, bytesRead);
                if (bytesRead > 0)
                {
                    lastActivity = DateTime.Now;
                    int bytesToSend;
                    lock (_decryptionLock)
                    {
                        if (_closed) return;
                        _encryptor.Decrypt(_remoteRecvBuffer, bytesRead, _remoteSendBuffer, out bytesToSend);
                    }
                    _connection.BeginSend(_remoteSendBuffer, 0, bytesToSend, SocketFlags.None, new AsyncCallback(PipeConnectionSendCallback), session);
                    IStrategy strategy = _controller.GetCurrentStrategy();
                    strategy?.UpdateLastRead(_server);
                }
                else
                {
                    _connection.Shutdown(SocketShutdown.Send);
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
                if(_connection == null) return;
                int bytesRead = _connection.EndReceive(ar);
                _totalWrite += bytesRead;

                var session = (AsyncSession<bool>) ar.AsyncState;
                var remote = session.Remote;

                if (bytesRead > 0)
                {
                    /*
                     * Only the first packet contains the socks5 header, it doesn't make sense to parse every packets. 
                     * Also it's unnecessary to parse these data if we turn off the VerboseLogging.
                     */
                    if (session.State && _config.isVerboseLogging)
                    {
                        int atyp = _connetionRecvBuffer[0];
                        string dst_addr;
                        int dst_port;
                        switch (atyp)
                        {
                            case 1: // IPv4 address, 4 bytes
                                dst_addr = new IPAddress(_connetionRecvBuffer.Skip(1).Take(4).ToArray()).ToString();
                                dst_port = (_connetionRecvBuffer[5] << 8) + _connetionRecvBuffer[6];

                                Logging.Info($"connect to {dst_addr}:{dst_port}");
                                session.State = false;
                                break;
                            case 3: // domain name, length + str
                                int len = _connetionRecvBuffer[1];
                                dst_addr = System.Text.Encoding.UTF8.GetString(_connetionRecvBuffer, 2, len);
                                dst_port = (_connetionRecvBuffer[len + 2] << 8) + _connetionRecvBuffer[len + 3];

                                Logging.Info($"connect to {dst_addr}:{dst_port}");
                                session.State = false;
                                break;
                            case 4: // IPv6 address, 16 bytes
                                dst_addr = new IPAddress(_connetionRecvBuffer.Skip(1).Take(16).ToArray()).ToString();
                                dst_port = (_connetionRecvBuffer[17] << 8) + _connetionRecvBuffer[18];

                                Logging.Info($"connect to [{dst_addr}]:{dst_port}");
                                session.State = false;
                                break;
                        }
                    }

                    int bytesToSend;
                    lock (_encryptionLock)
                    {
                        if (_closed) return;
                        _encryptor.Encrypt(_connetionRecvBuffer, bytesRead, _connetionSendBuffer, out bytesToSend);
                    }
                    _tcprelay.UpdateOutboundCounter(_server, bytesToSend);
                    _startSendingTime = DateTime.Now;
                    remote.BeginSend(_connetionSendBuffer, 0, bytesToSend, SocketFlags.None, new AsyncCallback(PipeRemoteSendCallback), session);
                    IStrategy strategy = _controller.GetCurrentStrategy();
                    strategy?.UpdateLastWrite(_server);
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
                var session = (AsyncSession)ar.AsyncState;
                session.Remote.EndSend(ar);
                _connection?.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeConnectionReceiveCallback), session);
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
                var session = (AsyncSession)ar.AsyncState;
                _connection?.EndSend(ar);
                session.Remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(PipeRemoteReceiveCallback), session);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }
    }
}
