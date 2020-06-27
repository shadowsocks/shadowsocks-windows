using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Timers;

using NLog;

using Shadowsocks.Controller.Strategy;
using Shadowsocks.Encryption;
using Shadowsocks.Encryption.AEAD;
using Shadowsocks.Encryption.Exception;
using Shadowsocks.Model;
using Shadowsocks.Proxy;
using Shadowsocks.Util.Sockets;

using static Shadowsocks.Encryption.EncryptorBase;

namespace Shadowsocks.Controller
{
    internal class TCPRelay : Listener.Service
    {
        public event EventHandler<SSTCPConnectedEventArgs> OnConnected;
        public event EventHandler<SSTransmitEventArgs> OnInbound;
        public event EventHandler<SSTransmitEventArgs> OnOutbound;
        public event EventHandler<SSRelayEventArgs> OnFailed;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ShadowsocksController _controller;
        private DateTime _lastSweepTime;
        private readonly Configuration _config;

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
            {
                return false;
            }

            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            TCPHandler handler = new TCPHandler(_controller, _config, socket);

            handler.OnConnected += OnConnected;
            handler.OnInbound += OnInbound;
            handler.OnOutbound += OnOutbound;
            handler.OnFailed += OnFailed;
            handler.OnClosed += (h, arg) =>
            {
                lock (Handlers)
                {
                    Handlers.Remove(handler);
                }
            };

            IList<TCPHandler> handlersToClose = new List<TCPHandler>();
            lock (Handlers)
            {
                Handlers.Add(handler);
                DateTime now = DateTime.Now;
                if (now - _lastSweepTime > TimeSpan.FromSeconds(1))
                {
                    _lastSweepTime = now;
                    foreach (TCPHandler handler1 in Handlers)
                    {
                        if (now - handler1.lastActivity > TimeSpan.FromSeconds(900))
                        {
                            handlersToClose.Add(handler1);
                        }
                    }
                }
            }
            foreach (TCPHandler handler1 in handlersToClose)
            {
                logger.Debug("Closing timed out TCP connection.");
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
    }

    public class SSRelayEventArgs : EventArgs
    {
        public readonly Server server;

        public SSRelayEventArgs(Server server)
        {
            this.server = server;
        }
    }

    public class SSTransmitEventArgs : SSRelayEventArgs
    {
        public readonly long length;
        public SSTransmitEventArgs(Server server, long length) : base(server)
        {
            this.length = length;
        }
    }

    public class SSTCPConnectedEventArgs : SSRelayEventArgs
    {
        public readonly TimeSpan latency;

        public SSTCPConnectedEventArgs(Server server, TimeSpan latency) : base(server)
        {
            this.latency = latency;
        }
    }

    internal class TCPHandler
    {
        public event EventHandler<SSTCPConnectedEventArgs> OnConnected;
        public event EventHandler<SSTransmitEventArgs> OnInbound;
        public event EventHandler<SSTransmitEventArgs> OnOutbound;
        public event EventHandler<SSRelayEventArgs> OnClosed;
        public event EventHandler<SSRelayEventArgs> OnFailed;

        private class AsyncSession
        {
            public IProxy Remote { get; }

            public AsyncSession(IProxy remote)
            {
                Remote = remote;
            }
        }

        private class AsyncSession<T> : AsyncSession
        {
            public T State { get; set; }

            public AsyncSession(IProxy remote, T state) : base(remote)
            {
                State = state;
            }

            public AsyncSession(AsyncSession session, T state) : base(session.Remote)
            {
                State = state;
            }
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly int _serverTimeout;
        private readonly int _proxyTimeout;

        // each recv size.
        public const int RecvSize = 2048;

        // overhead of one chunk, reserved for AEAD ciphers
        public const int ChunkOverheadSize = 16 * 2 /* two tags */ + AEADEncryptor.CHUNK_LEN_BYTES;

        // max chunk size
        public const uint MaxChunkSize = AEADEncryptor.CHUNK_LEN_MASK + AEADEncryptor.CHUNK_LEN_BYTES + 16 * 2;

        // In general, the ciphertext length, we should take overhead into account
        public const int BufferSize = RecvSize + (int)MaxChunkSize + 32 /* max salt len */;

        public DateTime lastActivity;

        private readonly ShadowsocksController _controller;
        private readonly ProxyConfig _config;
        private readonly Socket _connection;

        private IEncryptor _encryptor;
        private Server _server;

        private AsyncSession _currentRemoteSession;

        private bool _proxyConnected;
        private bool _destConnected;

        private byte _command;
        private byte[] _firstPacket;
        private int _firstPacketLength;

        private const int CMD_CONNECT = 0x01;
        private const int CMD_BIND = 0x02;
        private const int CMD_UDP_ASSOC = 0x03;

        private int _addrBufLength = -1;

        private int _totalRead = 0;
        private int _totalWrite = 0;

        // remote -> local proxy (ciphertext, before decrypt)
        private readonly byte[] _remoteRecvBuffer = new byte[BufferSize];

        // client -> local proxy (plaintext, before encrypt)
        private readonly byte[] _connetionRecvBuffer = new byte[BufferSize];

        // local proxy -> remote (plaintext, after decrypt)
        private readonly byte[] _remoteSendBuffer = new byte[BufferSize];

        // local proxy -> client (ciphertext, before decrypt)
        private readonly byte[] _connetionSendBuffer = new byte[BufferSize];

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

        // TODO: decouple controller
        public TCPHandler(ShadowsocksController controller, Configuration config, Socket socket)
        {
            _controller = controller;
            _config = config.proxy;
            _connection = socket;
            _proxyTimeout = config.proxy.proxyTimeout * 1000;
            _serverTimeout = config.GetCurrentServer().timeout * 1000;

            lastActivity = DateTime.Now;
        }

        public void CreateRemote()
        {
            Server server = _controller.GetAServer(IStrategyCallerType.TCP, (IPEndPoint)_connection.RemoteEndPoint,
                _destEndPoint);
            if (server == null || server.server == "")
            {
                throw new ArgumentException("No server configured");
            }

            _encryptor = EncryptorFactory.GetEncryptor(server.method, server.password);

            _server = server;

            /* prepare address buffer length for AEAD */
            Logger.Trace($"_addrBufLength={_addrBufLength}");
            _encryptor.AddrBufLength = _addrBufLength;
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
            {
                Close();
            }
        }

        private void ErrorClose(Exception e)
        {
            Logger.LogUsefulException(e);
            Close();
        }

        public void Close()
        {
            lock (_closeConnLock)
            {
                if (_closed)
                {
                    return;
                }

                _closed = true;
            }

            OnClosed?.Invoke(this, new SSRelayEventArgs(_server));

            try
            {
                _connection.Shutdown(SocketShutdown.Both);
                _connection.Close();
            }
            catch (Exception e)
            {
                Logger.LogUsefulException(e);
            }

            if (_currentRemoteSession != null)
            {
                try
                {
                    IProxy remote = _currentRemoteSession.Remote;
                    remote.Shutdown(SocketShutdown.Both);
                    remote.Close();
                }
                catch (Exception e)
                {
                    Logger.LogUsefulException(e);
                }
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
            if (_closed)
            {
                return;
            }

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
                        Logger.Error("socks 5 protocol error");
                    }
                    _connection.BeginSend(response, 0, response.Length, SocketFlags.None,
                        HandshakeSendCallback, null);
                }
                else
                {
                    Close();
                }
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private void HandshakeSendCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                _connection.EndSend(ar);

                // +-----+-----+-------+------+----------+----------+
                // | VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +-----+-----+-------+------+----------+----------+
                // |  1  |  1  | X'00' |  1   | Variable |    2     |
                // +-----+-----+-------+------+----------+----------+
                // Skip first 3 bytes, and read 2 more bytes to analysis the address.
                // 2 more bytes is designed if address is domain then we don't need to read once more to get the addr length.
                // validate is unnecessary, we did it in first packet, but we can do it in future version
                _connection.BeginReceive(_connetionRecvBuffer, 0, 3 + ADDR_ATYP_LEN + 1, SocketFlags.None,
                    AddressReceiveCallback, null);
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private void AddressReceiveCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                int bytesRead = _connection.EndReceive(ar);
                if (bytesRead >= 5)
                {
                    _command = _connetionRecvBuffer[1];
                    switch (_command)
                    {
                        case CMD_CONNECT:

                            // +----+-----+-------+------+----------+----------+
                            // |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
                            // +----+-----+-------+------+----------+----------+
                            // | 1  |  1  | X'00' |  1   | Variable |    2     |
                            // +----+-----+-------+------+----------+----------+
                            byte[] response = { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
                            _connection.BeginSend(response, 0, response.Length, SocketFlags.None,
                                ConnectResponseCallback, null);
                            break;
                        case CMD_UDP_ASSOC:
                            ReadAddress(HandleUDPAssociate);
                            break;
                        case CMD_BIND:  // not implemented
                        default:
                            Logger.Debug("Unsupported CMD=" + _command);
                            Close();
                            break;
                    }
                }
                else
                {
                    Logger.Debug(
                        "failed to recv data in Shadowsocks.Controller.TCPHandler.handshakeReceive2Callback()");
                    Close();
                }
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private void ConnectResponseCallback(IAsyncResult ar)
        {
            try
            {
                _connection.EndSend(ar);

                ReadAddress(StartConnect);
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private void ReadAddress(Action onSuccess)
        {
            int atyp = _connetionRecvBuffer[3];

            switch (atyp)
            {
                case ATYP_IPv4: // IPv4 address, 4 bytes
                    ReadAddress(4 + ADDR_PORT_LEN - 1, onSuccess);
                    break;
                case ATYP_DOMAIN: // domain name, length + str
                    int len = _connetionRecvBuffer[4];
                    ReadAddress(len + ADDR_PORT_LEN, onSuccess);
                    break;
                case ATYP_IPv6: // IPv6 address, 16 bytes
                    ReadAddress(16 + ADDR_PORT_LEN - 1, onSuccess);
                    break;
                default:
                    Logger.Debug("Unsupported ATYP=" + atyp);
                    Close();
                    break;
            }
        }

        private void ReadAddress(int bytesRemain, Action onSuccess)
        {
            // drop [ VER | CMD |  RSV  ]
            Array.Copy(_connetionRecvBuffer, 3, _connetionRecvBuffer, 0, ADDR_ATYP_LEN + 1);

            // Read the remain address bytes
            _connection.BeginReceive(_connetionRecvBuffer, 2, RecvSize - 2, SocketFlags.None, OnAddressFullyRead,
                new object[] { bytesRemain, onSuccess });
        }

        private void OnAddressFullyRead(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                int bytesRead = _connection.EndReceive(ar);

                object[] states = (object[])ar.AsyncState;

                int bytesRemain = (int)states[0];
                Action onSuccess = (Action)states[1];

                if (bytesRead >= bytesRemain)
                {
                    _firstPacketLength = bytesRead + 2;

                    int atyp = _connetionRecvBuffer[0];

                    string dstAddr = "Unknown";
                    int dstPort = -1;
                    switch (atyp)
                    {
                        case ATYP_IPv4: // IPv4 address, 4 bytes
                            dstAddr = new IPAddress(_connetionRecvBuffer.Skip(1).Take(4).ToArray()).ToString();
                            dstPort = (_connetionRecvBuffer[5] << 8) + _connetionRecvBuffer[6];

                            _addrBufLength = ADDR_ATYP_LEN + 4 + ADDR_PORT_LEN;
                            break;
                        case ATYP_DOMAIN: // domain name, length + str
                            int len = _connetionRecvBuffer[1];
                            dstAddr = System.Text.Encoding.UTF8.GetString(_connetionRecvBuffer, 2, len);
                            dstPort = (_connetionRecvBuffer[len + 2] << 8) + _connetionRecvBuffer[len + 3];

                            _addrBufLength = ADDR_ATYP_LEN + 1 + len + ADDR_PORT_LEN;
                            break;
                        case ATYP_IPv6: // IPv6 address, 16 bytes
                            dstAddr = $"[{new IPAddress(_connetionRecvBuffer.Skip(1).Take(16).ToArray())}]";
                            dstPort = (_connetionRecvBuffer[17] << 8) + _connetionRecvBuffer[18];

                            _addrBufLength = ADDR_ATYP_LEN + 16 + ADDR_PORT_LEN;
                            break;
                    }

                    Logger.Debug($"connect to {dstAddr}:{dstPort}");

                    _destEndPoint = SocketUtil.GetEndPoint(dstAddr, dstPort);

                    onSuccess.Invoke(); /* StartConnect() */
                }
                else
                {
                    Logger.Debug("failed to recv data in Shadowsocks.Controller.TCPHandler.OnAddressFullyRead()");
                    Close();
                }
            }
            catch (Exception e)
            {
                ErrorClose(e);
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
            if (_closed)
            {
                return;
            }

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
                    {
                        Close();
                    }
                }
            }
            catch (Exception e)
            {
                ErrorClose(e);
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

            public ServerTimer(int p) : base(p)
            {
            }
        }

        private void StartConnect()
        {
            try
            {
                CreateRemote();

                // Setting up proxy
                IProxy remote;
                EndPoint proxyEP = null;
                EndPoint serverEP = SocketUtil.GetEndPoint(_server.server, _server.server_port);
                EndPoint pluginEP = _controller.GetPluginLocalEndPointIfConfigured(_server);

                if (pluginEP != null)
                {
                    serverEP = pluginEP;
                    remote = new DirectConnect();
                }
                else if (_config.useProxy)
                {
                    switch (_config.proxyType)
                    {
                        case ProxyConfig.PROXY_SOCKS5:
                            remote = new Socks5Proxy();
                            break;
                        case ProxyConfig.PROXY_HTTP:
                            remote = new HttpProxy();
                            break;
                        default:
                            throw new NotSupportedException("Unknown forward proxy.");
                    }
                    proxyEP = SocketUtil.GetEndPoint(_config.proxyServer, _config.proxyPort);
                }
                else
                {
                    remote = new DirectConnect();
                }

                AsyncSession session = new AsyncSession(remote);
                lock (_closeConnLock)
                {
                    if (_closed)
                    {
                        remote.Close();
                        return;
                    }

                    _currentRemoteSession = session;
                }

                ProxyTimer proxyTimer = new ProxyTimer(_proxyTimeout) { AutoReset = false };
                proxyTimer.Elapsed += ProxyConnectTimer_Elapsed;
                proxyTimer.Enabled = true;

                proxyTimer.Session = session;
                proxyTimer.DestEndPoint = serverEP;
                proxyTimer.Server = _server;

                _proxyConnected = false;

                // Connect to the proxy server.
                remote.BeginConnectProxy(proxyEP, ProxyConnectCallback,
                    new AsyncSession<ProxyTimer>(remote, proxyTimer));
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private void ProxyConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ProxyTimer timer = (ProxyTimer)sender;
            timer.Elapsed -= ProxyConnectTimer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();


            if (_proxyConnected || _destConnected || _closed)
            {
                return;
            }
            IProxy proxy = timer.Session.Remote;

            Logger.Info($"Proxy {proxy.ProxyEndPoint} timed out");
            proxy.Close();
            Close();
        }

        private void ProxyConnectCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }
            try
            {
                AsyncSession<ProxyTimer> session = (AsyncSession<ProxyTimer>)ar.AsyncState;
                ProxyTimer timer = session.State;
                EndPoint destEndPoint = timer.DestEndPoint;
                Server server = timer.Server;
                timer.Elapsed -= ProxyConnectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();

                IProxy remote = session.Remote;

                // Complete the connection.
                remote.EndConnectProxy(ar);

                _proxyConnected = true;

                if (!(remote is DirectConnect))
                {
                    Logger.Debug($"Socket connected to proxy {remote.ProxyEndPoint}");
                }

                _startConnectTime = DateTime.Now;
                ServerTimer connectTimer = new ServerTimer(_serverTimeout) { AutoReset = false };
                connectTimer.Elapsed += DestConnectTimer_Elapsed;
                connectTimer.Enabled = true;
                connectTimer.Session = session;
                connectTimer.Server = server;

                _destConnected = false;

                NetworkCredential auth = null;
                if (_config.useAuth)
                {
                    auth = new NetworkCredential(_config.authUser, _config.authPwd);
                }

                // Connect to the remote endpoint.
                remote.BeginConnectDest(destEndPoint, ConnectCallback,
                    new AsyncSession<ServerTimer>(session, connectTimer), auth);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private void DestConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ServerTimer timer = (ServerTimer)sender;
            timer.Elapsed -= DestConnectTimer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();

            if (_destConnected || _closed)
            {
                return;
            }

            AsyncSession session = timer.Session;
            Server server = timer.Server;
            OnFailed?.Invoke(this, new SSRelayEventArgs(_server));
            Logger.Info($"{server.ToString()} timed out");
            session.Remote.Close();
            Close();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                AsyncSession<ServerTimer> session = (AsyncSession<ServerTimer>)ar.AsyncState;
                ServerTimer timer = session.State;
                _server = timer.Server;
                timer.Elapsed -= DestConnectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();

                IProxy remote = session.Remote;
                // Complete the connection.
                remote.EndConnectDest(ar);

                _destConnected = true;

                Logger.Debug($"Socket connected to ss server: {_server.ToString()}");

                TimeSpan latency = DateTime.Now - _startConnectTime;

                OnConnected?.Invoke(this, new SSTCPConnectedEventArgs(_server, latency));

                StartPipe(session);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                if (_server != null)
                {
                    OnFailed?.Invoke(this, new SSRelayEventArgs(_server));
                }
                ErrorClose(e);
            }
        }

        private void TryReadAvailableData()
        {
            int available = Math.Min(_connection.Available, RecvSize - _firstPacketLength);
            if (available > 0)
            {
                int size = _connection.Receive(_connetionRecvBuffer, _firstPacketLength, available,
                    SocketFlags.None);

                _firstPacketLength += size;
            }
        }

        private void StartPipe(AsyncSession session)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                _startReceivingTime = DateTime.Now;
                session.Remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None,
                    PipeRemoteReceiveCallback, session);

                TryReadAvailableData();
                Logger.Trace($"_firstPacketLength = {_firstPacketLength}");
                SendToServer(_firstPacketLength, session);
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private void PipeRemoteReceiveCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                AsyncSession session = (AsyncSession)ar.AsyncState;
                int bytesRead = session.Remote.EndReceive(ar);
                _totalRead += bytesRead;

                OnInbound?.Invoke(this, new SSTransmitEventArgs(_server, bytesRead));
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
                        catch (CryptoErrorException)
                        {
                            Logger.Error("decryption error");
                            Close();
                            return;
                        }
                    }
                    if (bytesToSend == 0)
                    {
                        // need more to decrypt
                        Logger.Trace("Need more to decrypt");
                        session.Remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None,
                            PipeRemoteReceiveCallback, session);
                        return;
                    }
                    Logger.Trace($"start sending {bytesToSend}");
                    _connection.BeginSend(_remoteSendBuffer, 0, bytesToSend, SocketFlags.None,
                        PipeConnectionSendCallback, new object[] { session, bytesToSend });
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
                ErrorClose(e);
            }
        }

        private void PipeConnectionReceiveCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                int bytesRead = _connection.EndReceive(ar);

                AsyncSession session = (AsyncSession)ar.AsyncState;
                IProxy remote = session.Remote;

                if (bytesRead > 0)
                {
                    SendToServer(bytesRead, session);
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
                ErrorClose(e);
            }
        }

        private void SendToServer(int length, AsyncSession session)
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
                    Logger.Debug("encryption error");
                    Close();
                    return;
                }
            }

            OnOutbound?.Invoke(this, new SSTransmitEventArgs(_server, bytesToSend));
            _startSendingTime = DateTime.Now;
            session.Remote.BeginSend(_connetionSendBuffer, 0, bytesToSend, SocketFlags.None,
                PipeRemoteSendCallback, new object[] { session, bytesToSend });
        }

        private void PipeRemoteSendCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                object[] container = (object[])ar.AsyncState;
                AsyncSession session = (AsyncSession)container[0];
                int bytesShouldSend = (int)container[1];
                int bytesSent = session.Remote.EndSend(ar);

                if (bytesSent > 0)
                {
                    lastActivity = DateTime.Now;
                }

                int bytesRemaining = bytesShouldSend - bytesSent;
                if (bytesRemaining > 0)
                {
                    Logger.Info("reconstruct _connetionSendBuffer to re-send");
                    Buffer.BlockCopy(_connetionSendBuffer, bytesSent, _connetionSendBuffer, 0, bytesRemaining);
                    session.Remote.BeginSend(_connetionSendBuffer, 0, bytesRemaining, SocketFlags.None,
                        PipeRemoteSendCallback, new object[] { session, bytesRemaining });
                    return;
                }
                _connection.BeginReceive(_connetionRecvBuffer, 0, RecvSize, SocketFlags.None,
                    PipeConnectionReceiveCallback, session);
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        // In general, we assume there is no delay between local proxy and client, add this for sanity
        private void PipeConnectionSendCallback(IAsyncResult ar)
        {
            try
            {
                object[] container = (object[])ar.AsyncState;
                AsyncSession session = (AsyncSession)container[0];
                int bytesShouldSend = (int)container[1];
                int bytesSent = _connection.EndSend(ar);
                int bytesRemaining = bytesShouldSend - bytesSent;
                if (bytesRemaining > 0)
                {
                    Logger.Info("reconstruct _remoteSendBuffer to re-send");
                    Buffer.BlockCopy(_remoteSendBuffer, bytesSent, _remoteSendBuffer, 0, bytesRemaining);
                    _connection.BeginSend(_remoteSendBuffer, 0, bytesRemaining, SocketFlags.None,
                        PipeConnectionSendCallback, new object[] { session, bytesRemaining });
                    return;
                }
                session.Remote.BeginReceive(_remoteRecvBuffer, 0, RecvSize, SocketFlags.None,
                    PipeRemoteReceiveCallback, session);
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }
    }
}