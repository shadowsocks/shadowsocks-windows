using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Shadowsocks.Controller.Strategy;
using Shadowsocks.Encryption;
using Shadowsocks.Encryption.AEAD;
using Shadowsocks.Encryption.Exception;
using Shadowsocks.Model;
using Shadowsocks.Util.Sockets;
using static Shadowsocks.Encryption.EncryptorBase;

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
            handlersToClose.ForEach(h => h.Close());
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

    internal class TCPHandler
    {
        private readonly int _serverTimeout;

        // each recv size.
        public const int RecvSize = 2048;

        // overhead of one chunk, reserved for AEAD ciphers
        public const int ChunkOverheadSize = 16 * 2 /* two tags */ + AEADEncryptor.CHUNK_LEN_BYTES;

        // max chunk size
        public const uint MaxChunkSize = AEADEncryptor.CHUNK_LEN_MASK + AEADEncryptor.CHUNK_LEN_BYTES + 16 * 2;

        // In general, the ciphertext length, we should take overhead into account
        public const int BufferSize = RecvSize + (int)MaxChunkSize + 32 /* max salt len */;

        public static readonly byte[] Sock5HandshakeResponseReject = { 0, 0x5B /* other bytes are ignored */};

        //+----+--------+
        //|VER | METHOD |
        //+----+--------+
        //| 1  |   1    |
        //+----+--------+
        public static readonly byte[] Sock5HandshakeResponseSuccess = { 5, 0 /* no auth required */};

        //+----+-----+-------+------+----------+----------+
        //|VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
        //+----+-----+-------+------+----------+----------+
        //| 1  |  1  | X'00' |  1   | Variable |    2     |
        //+----+-----+-------+------+----------+----------+
        public static readonly byte[] Sock5ConnectRequestReplySuccess = { 5, 0, 0, ATYP_IPv4, 0, 0, 0, 0, 0, 0 };

        public DateTime lastActivity;

        private ShadowsocksController _controller;
        private Configuration _config;
        private TCPRelay _tcprelay;
        private Socket _connection;
        private Socket _remote;

        private IEncryptor _encryptor;
        private Server _server;

        private bool _destConnected;

        private byte[] _firstPacket;
        private int _firstPacketLength;

        private const int CMD_CONNECT = 0x01;
        private const int CMD_UDP_ASSOC = 0x03;

        // <real-addr-buf>[<additional-data>]
        private byte[] _addrBuf;

        // the real addr buffer length
        private int _addrBufLength = -1;

        private string dstAddr = "Unknown";
        private int dstPort = -1;

        private int _totalRead = 0;
        private int _totalWrite = 0;

        // remote -> local proxy (ciphertext, before decrypt)
        private byte[] _remoteRecvBuffer = new byte[BufferSize];

        // client -> local proxy (plaintext, before encrypt)
        private byte[] _connetionRecvBuffer = new byte[BufferSize];

        // local proxy -> remote (plaintext, after decrypt)
        private byte[] _remoteSendBuffer = new byte[BufferSize];

        // local proxy -> client (ciphertext, before decrypt)
        private byte[] _connetionSendBuffer = new byte[BufferSize];

        private bool _connectionShutdown = false;
        private bool _remoteShutdown = false;
        private bool _closed = false;

        // instance-based lock without static
        private readonly object _encryptionLock = new object();

        private readonly object _decryptionLock = new object();
        private readonly object _closeConnLock = new object();

        private DateTime _startConnectTime;
        private DateTime _startReceivingTime;
        private DateTime _startSendingTime;

        private EndPoint _destEndPoint = null;

        public TCPHandler(ShadowsocksController controller, Configuration config, TCPRelay tcprelay, Socket socket)
        {
            _controller = controller;
            _config = config;
            _tcprelay = tcprelay;
            _connection = socket;
            _serverTimeout = config.GetCurrentServer().timeout * 1000;

            lastActivity = DateTime.Now;
        }

        public void CreateRemote()
        {
            Server server = _controller.GetAServer(IStrategyCallerType.TCP, (IPEndPoint)_connection.RemoteEndPoint,
                _destEndPoint);
            if (server == null || server.server == "")
                throw new ArgumentException("No server configured");

            _encryptor = EncryptorFactory.GetEncryptor(server.method, server.password);

            this._server = server;

            /* prepare address buffer length for AEAD */
            Logging.Debug($"_addrBufLength={_addrBufLength}");
            _encryptor.AddrBufLength = _addrBufLength;
        }

        public void Start(byte[] firstPacket, int length)
        {
            _firstPacket = firstPacket;
            _firstPacketLength = length;
            HandshakeReceive();
        }

        private void HandshakeReceive()
        {
            if (_closed) return;
            try
            {
                int bytesRead = _firstPacketLength;
                if (bytesRead > 1)
                {
                    byte[] response = Sock5HandshakeResponseSuccess;
                    if (_firstPacket[0] != 5)
                    {
                        // reject socks 4
                        response = Sock5HandshakeResponseReject;
                        Logging.Error("socks 5 protocol error");
                    }
                    _connection.BeginSend(response, 0, response.Length, SocketFlags.None,
                        HandshakeSendCallback, null);
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
                _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None,
                    HandshakeReceive2Callback, null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void HandshakeReceive2Callback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                int bytesRead = _connection.EndReceive(ar);
                if (bytesRead >= 5)
                {
                    var _command = _connetionRecvBuffer[1];
                    if (_command != CMD_CONNECT && _command != CMD_UDP_ASSOC)
                    {
                        Logging.Debug("Unsupported CMD=" + _command);
                        Close();
                    }
                    else
                    {
                        ParseAddrBuf(_connetionRecvBuffer, bytesRead);

                        if (_command == CMD_CONNECT)
                        {
                            _connection.BeginSend(Sock5ConnectRequestReplySuccess, 0,
                                Sock5ConnectRequestReplySuccess.Length, SocketFlags.None,
                                ResponseCallback, null);
                        }
                        else if (_command == CMD_UDP_ASSOC)
                        {
                            HandleUDPAssociate();
                        }
                    }
                }
                else
                {
                    Logging.Debug(
                        "failed to recv data in Shadowsocks.Controller.TCPHandler.handshakeReceive2Callback()");
                    Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void ParseAddrBuf(byte[] buf, int length)
        {
            Logging.Debug("enter ParseAddrBuf");
            _addrBuf = buf.Skip(3).Take(length - 3).ToArray();
            Logging.Dump("recvBuf", buf, length);
            Logging.Dump(nameof(_addrBuf), _addrBuf, _addrBuf.Length);
            int atyp = _addrBuf[0];

            switch (atyp)
            {
                case ATYP_IPv4: // IPv4 address, 4 bytes
                    dstAddr = new IPAddress(_addrBuf.Skip(1).Take(4).ToArray()).ToString();
                    dstPort = (_addrBuf[5] << 8) + _addrBuf[6];

                    _addrBufLength = ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                    break;
                case ATYP_DOMAIN: // domain name, length + str
                    int len = _addrBuf[1];
                    dstAddr = System.Text.Encoding.UTF8.GetString(_addrBuf, 2, len);
                    dstPort = (_addrBuf[len + 2] << 8) + _addrBuf[len + 3];

                    _addrBufLength = ADDR_ATYP_LEN + 1 + len + ADDR_PORT_LEN;
                    break;
                case ATYP_IPv6: // IPv6 address, 16 bytes
                    dstAddr = $"[{new IPAddress(_addrBuf.Skip(1).Take(16).ToArray())}]";
                    dstPort = (_addrBuf[17] << 8) + _addrBuf[18];

                    _addrBufLength = ADDR_ATYP_LEN + 16 + ADDR_PORT_LEN;
                    break;
            }
            Logging.Debug(nameof(_addrBufLength) + " " + _addrBufLength);

            _destEndPoint = SocketUtil.GetEndPoint(dstAddr, dstPort);

            if (_config.isVerboseLogging)
            {
                Logging.Info($"connect to {dstAddr}:{dstPort}");
            }
        }

        private void ResponseCallback(IAsyncResult ar)
        {
            try
            {
                _connection.EndSend(ar);

                StartConnect();
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
            byte[] response = new byte[4 + address.Length + ADDR_PORT_LEN];
            response[0] = 5;
            switch (endPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    response[3] = ATYP_IPv4;
                    break;
                case AddressFamily.InterNetworkV6:
                    response[3] = ATYP_IPv6;
                    break;
            }
            address.CopyTo(response, 4);
            response[response.Length - 1] = (byte)(port & 0xFF);
            response[response.Length - 2] = (byte)((port >> 8) & 0xFF);
            _connection.BeginSend(response, 0, response.Length, SocketFlags.None, ReadAll, true);
        }

        private void ReadAll(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {
                if (ar.AsyncState != null)
                {
                    _connection.EndSend(ar);
                    _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None,
                        ReadAll, null);
                }
                else
                {
                    int bytesRead = _connection.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None,
                            ReadAll, null);
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

        // inner class
        private class ServerTimer : Timer
        {
            public Server Server;

            public SocketAsyncEventArgs Args;

            public ServerTimer(int p) : base(p)
            {
            }
        }

        private void StartConnect()
        {
            try
            {
                CreateRemote();

                // let SAEA's RemoteEndPoint determine the AddressFamily
                _remote = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                _remote.SetTFO();

                lock (_closeConnLock)
                {
                    if (_closed)
                    {
                        _remote.Close();
                        return;
                    }
                }

                var encBytesLen = -1;
                var encBuf = new byte[BufferSize];
                // encrypt addr buf
                lock (_encryptionLock)
                {
                    try
                    {
                        _encryptor.Encrypt(_addrBuf, _addrBuf.Length, encBuf, out encBytesLen);
                    }
                    catch (CryptoErrorException)
                    {
                        Logging.Debug("encryption error");
                        throw;
                    }
                }

                // Connect to the proxy server.
                _startConnectTime = DateTime.Now;
                ServerTimer connectTimer = new ServerTimer(_serverTimeout) { AutoReset = false };
                connectTimer.Elapsed += DestConnectTimer_Elapsed;
                connectTimer.Enabled = true;

                var args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = SocketUtil.GetEndPoint(_server.server, _server.server_port);
                args.Completed += ConnectCallback;
                args.UserToken = connectTimer;
                args.SetBuffer(encBuf, 0, encBytesLen);

                connectTimer.Args = args;
                connectTimer.Server = _server;


                _destConnected = false;
                // Connect to the remote endpoint.

                if (!_remote.ConnectAsync(args))
                {
                    ConnectCallback(_remote, args);
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void DestConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {

            var timer = (ServerTimer)sender;
            var args = timer.Args;
            args.Completed -= ConnectCallback;
            args.Dispose();
            timer.Elapsed -= DestConnectTimer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();

            if (_destConnected || _closed)
            {
                return;
            }

            Server server = timer.Server;
            IStrategy strategy = _controller.GetCurrentStrategy();
            strategy?.SetFailure(server);
            Logging.Info($"{server.FriendlyName()} timed out");
            Close();
        }

        private void ConnectCallback(object sender, SocketAsyncEventArgs e)
        {
            using (e)
            {
                if (_closed) return;
                try
                {
                    if (e.SocketError != SocketError.Success)
                    {
                        Logging.LogUsefulException(new Exception(e.SocketError.ToString()));
                        Close();
                        return;
                    }

                    if (e.BytesTransferred <= 0)
                    {
                        // close
                        Close();
                        return;
                    }

                    ServerTimer timer = (ServerTimer)e.UserToken;
                    timer.Enabled = false;
                    timer.Elapsed -= DestConnectTimer_Elapsed;
                    timer.Dispose();

                    if (_config.isVerboseLogging)
                    {
                        Logging.Info($"Socket connected to ss server: {_server.FriendlyName()}");
                    }

                    _destConnected = true;

                    if (_config.isVerboseLogging)
                    {
                        Logging.Info($"Socket connected to ss server: {_server.FriendlyName()}");
                    }

                    var latency = DateTime.Now - _startConnectTime;
                    IStrategy strategy = _controller.GetCurrentStrategy();
                    strategy?.UpdateLatency(_server, latency);
                    _tcprelay.UpdateLatency(_server, latency);

                    StartPipe();
                }
                catch (Exception ex)
                {
                    if (_server != null)
                    {
                        IStrategy strategy = _controller.GetCurrentStrategy();
                        strategy?.SetFailure(_server);
                    }
                    Logging.LogUsefulException(ex);
                    Close();
                }
            }
        }

        private void StartPipe()
        {
            if (_closed) return;
            try
            {
                _startReceivingTime = DateTime.Now;
                _remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None,
                    PipeRemoteReceiveCallback, null);

                _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None,
                    PipeConnectionReceiveCallback, null);
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
                int bytesRead = _remote.EndReceive(ar);
                _totalRead += bytesRead;
                _tcprelay.UpdateInboundCounter(_server, bytesRead);
                if (bytesRead > 0)
                {
                    lastActivity = DateTime.Now;
                    int bytesToSend = -1;
                    lock (_decryptionLock)
                    {
                        try
                        {
                            _encryptor.Decrypt(_remoteRecvBuffer, bytesRead, _remoteSendBuffer, out bytesToSend);
                        }
                        catch (CryptoErrorException e)
                        {
                            Logging.LogUsefulException(e);
                            Close();
                            return;
                        }
                    }
                    if (bytesToSend == 0)
                    {
                        // need more to decrypt
                        Logging.Debug("Need more to decrypt");
                        _remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None,
                            PipeRemoteReceiveCallback, null);
                        return;
                    }
                    Logging.Debug($"start sending {bytesToSend}");
                    _connection.BeginSend(_remoteSendBuffer, 0, bytesToSend, SocketFlags.None,
                        PipeConnectionSendCallback, bytesToSend);
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
                int bytesRead = _connection.EndReceive(ar);

                if (bytesRead > 0)
                {
                    SendToServer(bytesRead);
                }
                else
                {
                    _remote.Shutdown(SocketShutdown.Send);
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

        private void SendToServer(int length)
        {
            _totalWrite += length;
            int bytesToSend;
            lock (_encryptionLock)
            {
                try
                {
                    _encryptor.Encrypt(_connetionRecvBuffer, length, _connetionSendBuffer, out bytesToSend);
                }
                catch (CryptoErrorException)
                {
                    Logging.Debug("encryption error");
                    Close();
                    return;
                }
            }
            _tcprelay.UpdateOutboundCounter(_server, bytesToSend);
            _startSendingTime = DateTime.Now;
            _remote.BeginSend(_connetionSendBuffer, 0, bytesToSend, SocketFlags.None,
                PipeRemoteSendCallback, bytesToSend);
            IStrategy strategy = _controller.GetCurrentStrategy();
            strategy?.UpdateLastWrite(_server);
        }

        private void PipeRemoteSendCallback(IAsyncResult ar)
        {
            if (_closed) return;
            try
            {

                var bytesShouldSend = (int)ar.AsyncState;
                int bytesSent = _remote.EndSend(ar);
                int bytesRemaining = bytesShouldSend - bytesSent;
                if (bytesRemaining > 0)
                {
                    Logging.Info("reconstruct _connetionSendBuffer to re-send");
                    Buffer.BlockCopy(_connetionSendBuffer, bytesSent, _connetionSendBuffer, 0, bytesRemaining);
                    _remote.BeginSend(_connetionSendBuffer, 0, bytesRemaining, SocketFlags.None,
                        PipeRemoteSendCallback, bytesRemaining);
                    return;
                }
                _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None,
                    PipeConnectionReceiveCallback, null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        // In general, we assume there is no delay between local proxy and client, add this for sanity
        private void PipeConnectionSendCallback(IAsyncResult ar)
        {
            try
            {
                var bytesShouldSend = (int)ar.AsyncState;
                var bytesSent = _connection.EndSend(ar);
                var bytesRemaining = bytesShouldSend - bytesSent;
                if (bytesRemaining > 0)
                {
                    Logging.Info("reconstruct _remoteSendBuffer to re-send");
                    Buffer.BlockCopy(_remoteSendBuffer, bytesSent, _remoteSendBuffer, 0, bytesRemaining);
                    _connection.BeginSend(_remoteSendBuffer, 0, bytesRemaining, SocketFlags.None,
                        PipeConnectionSendCallback, bytesRemaining);
                    return;
                }
                _remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None,
                    PipeRemoteReceiveCallback, null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void CheckClose()
        {
            if (_connectionShutdown && _remoteShutdown)
                Close();
        }

        public void Close()
        {
            lock (_closeConnLock)
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
                _connection.Shutdown(SocketShutdown.Both);
                _connection.Close();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }

            try
            {
                _remote.Shutdown(SocketShutdown.Both);
                _remote.Close();
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
    }
}