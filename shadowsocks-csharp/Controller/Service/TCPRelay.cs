using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Shadowsocks.Controller.Strategy;
using Shadowsocks.Encryption;
using Shadowsocks.ForwardProxy;
using Shadowsocks.Model;
using Shadowsocks.Util.Sockets;

namespace Shadowsocks.Controller.Service
{
    class TCPRelay : Listener.Service
    {
        private ShadowsocksController _controller;
        private DateTime _lastSweepTime;
        private Configuration _config;


        private readonly List<ITCPHandlerFactory> _factories = new List<ITCPHandlerFactory>();

        public ISet<TCPHandler> Handlers { get; } = new HashSet<TCPHandler>();

        public TCPRelay(ShadowsocksController controller, Configuration conf)
        {
            _controller = controller;
            _config = conf;
            _lastSweepTime = DateTime.Now;

            _factories.Add(new Socks5HandlerFactory());
            _factories.Add(new HttpHandlerHandlerFactory());
        }

        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            TCPHandler handler = null;
            foreach (var factory in _factories)
            {
                if (factory.CanHandle(firstPacket, length))
                {
                    handler = factory.NewHandler(_controller, _config, this, socket);
                    break;
                }
            }

            if (handler == null)
            {
                return false;
            }

            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

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
            handler.StartHandshake(firstPacket, length);

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

    interface ITCPHandlerFactory
    {
        bool CanHandle(byte[] firstPacket, int length);

        TCPHandler NewHandler(ShadowsocksController controller, Configuration config, TCPRelay tcprelay, Socket socket);
    }

    abstract class TCPHandler
    {
        public abstract void StartHandshake(byte[] firstPacket, int length);

        protected abstract void OnServerConnected(AsyncSession session);


        protected class AsyncSession
        {
            public IForwardProxy Remote { get; }

            public AsyncSession(IForwardProxy remote)
            {
                Remote = remote;
            }
        }

        protected class AsyncSession<T> : AsyncSession
        {
            public T State { get; set; }

            public AsyncSession(IForwardProxy remote, T state) : base(remote)
            {
                State = state;
            }

            public AsyncSession(AsyncSession session, T state) : base(session.Remote)
            {
                State = state;
            }
        }

        private readonly int _serverTimeout;
        private readonly int _proxyTimeout;

        // Size of receive buffer.
        public static readonly int RecvSize = 8192;
        public static readonly int RecvReserveSize = IVEncryptor.ONETIMEAUTH_BYTES + IVEncryptor.AUTH_BYTES; // reserve for one-time auth
        public static readonly int BufferSize = RecvSize + RecvReserveSize + 32;

        public DateTime lastActivity;

        private ShadowsocksController _controller;
        protected Configuration Config { get; }
        private TCPRelay _tcprelay;
        protected Socket Connection { get; }

        private Server _server;
        private AsyncSession _currentRemoteSession;

        private bool _proxyConnected;
        private bool _destConnected;

        private int _totalRead = 0;
        private int _totalWrite = 0;

        protected byte[] RemoteRecvBuffer { get; } = new byte[BufferSize];
        private readonly byte[] _remoteSendBuffer = new byte[BufferSize];
        protected byte[] ConnetionRecvBuffer { get; } = new byte[BufferSize];
        private readonly byte[] _connetionSendBuffer = new byte[BufferSize];

        private IEncryptor _encryptor;
        private readonly object _encryptionLock = new object();
        private readonly object _decryptionLock = new object();

        private bool _connectionShutdown = false;
        private bool _remoteShutdown = false;
        protected bool Closed { get; private set; }= false;
        private readonly object _closeConnLock = new object();

        private DateTime _startConnectTime;
        private DateTime _startReceivingTime;
        private DateTime _startSendingTime;

        private EndPoint _destEndPoint = null;


        public TCPHandler(ShadowsocksController controller, Configuration config, TCPRelay tcprelay, Socket socket, bool autoAppendHeader = true)
        {
            _controller = controller;
            Config = config;
            _tcprelay = tcprelay;
            Connection = socket;
            _proxyTimeout = config.proxy.proxyTimeout * 1000;
            _serverTimeout = config.GetCurrentServer().timeout * 1000;

            lastActivity = DateTime.Now;

            _serverHeaderSent = !autoAppendHeader;
        }

        private void CreateRemote()
        {
            Server server = _controller.GetAServer(IStrategyCallerType.TCP, (IPEndPoint)Connection.RemoteEndPoint, _destEndPoint);
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

        private void CheckClose()
        {
            if (_connectionShutdown && _remoteShutdown)
                Close();
        }

        public void Close()
        {
            lock (_closeConnLock)
            {
                if (Closed) return;
                Closed = true;
            }
            lock (_tcprelay.Handlers)
            {
                _tcprelay.Handlers.Remove(this);
            }
            try
            {
                Connection.Shutdown(SocketShutdown.Both);
                Connection.Close();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }

            if (_currentRemoteSession != null)
            {
                try
                {
                    var remote = _currentRemoteSession.Remote;
                    remote.Shutdown(SocketShutdown.Both);
                    remote.Close();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
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

        protected void StartConnect(EndPoint target)
        {
            try
            {
                _destEndPoint = target;

                CreateRemote();

                // Setting up proxy
                IForwardProxy remote;
                EndPoint proxyEP;
                if (Config.proxy.useProxy)
                {
                    switch (Config.proxy.proxyType)
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
                    proxyEP = SocketUtil.GetEndPoint(Config.proxy.proxyServer, Config.proxy.proxyPort);
                }
                else
                {
                    remote = new DirectConnect();
                    proxyEP = null;
                }

                var session = new AsyncSession(remote);
                lock (_closeConnLock)
                {
                    if (Closed)
                    {
                        remote.Close();
                        return;
                    }

                    _currentRemoteSession = session;
                }

                ProxyTimer proxyTimer = new ProxyTimer(_proxyTimeout);
                proxyTimer.AutoReset = false;
                proxyTimer.Elapsed += proxyConnectTimer_Elapsed;
                proxyTimer.Enabled = true;

                proxyTimer.Session = session;
                proxyTimer.DestEndPoint = SocketUtil.GetEndPoint(_server.server, _server.server_port);
                proxyTimer.Server = _server;

                _proxyConnected = false;

                // Connect to the proxy server.
                remote.BeginConnectProxy(proxyEP, ProxyConnectCallback, new AsyncSession<ProxyTimer>(remote, proxyTimer));
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }


        private void proxyConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = (ProxyTimer)sender;
            timer.Elapsed -= proxyConnectTimer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();


            if (_proxyConnected || _destConnected || Closed)
            {
                return;
            }
            var proxy = timer.Session.Remote;

            Logging.Info($"Proxy {proxy.ProxyEndPoint} timed out");
            proxy.Close();
            Close();
        }

        private void ProxyConnectCallback(IAsyncResult ar)
        {
            Server server = null;
            if (Closed)
            {
                return;
            }
            try
            {
                var session = (AsyncSession<ProxyTimer>)ar.AsyncState;
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

                if (Config.isVerboseLogging)
                {
                    if (!(remote is DirectConnect))
                    {
                        Logging.Info($"Socket connected to proxy {remote.ProxyEndPoint}");
                    }
                }

                _startConnectTime = DateTime.Now;
                ServerTimer connectTimer = new ServerTimer(_serverTimeout);
                connectTimer.AutoReset = false;
                connectTimer.Elapsed += destConnectTimer_Elapsed;
                connectTimer.Enabled = true;
                connectTimer.Session = session;
                connectTimer.Server = server;

                _destConnected = false;
                // Connect to the remote endpoint.
                remote.BeginConnectDest(destEndPoint, ConnectCallback, new AsyncSession<ServerTimer>(session, connectTimer));
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void destConnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var timer = (ServerTimer)sender;
            timer.Elapsed -= destConnectTimer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();

            if (_destConnected || Closed)
            {
                return;
            }

            var session = timer.Session;
            Server server = timer.Server;
            IStrategy strategy = _controller.GetCurrentStrategy();
            strategy?.SetFailure(server);
            Logging.Info($"{server.FriendlyName()} timed out");
            session.Remote.Close();
            Close();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            if (Closed) return;
            try
            {
                var session = (AsyncSession<ServerTimer>)ar.AsyncState;
                ServerTimer timer = session.State;
                _server = timer.Server;
                timer.Elapsed -= destConnectTimer_Elapsed;
                timer.Enabled = false;
                timer.Dispose();

                var remote = session.Remote;
                // Complete the connection.
                remote.EndConnectDest(ar);

                _destConnected = true;

                if (Config.isVerboseLogging)
                {
                    Logging.Info($"Socket connected to ss server: {_server.FriendlyName()}");
                }

                var latency = DateTime.Now - _startConnectTime;
                IStrategy strategy = _controller.GetCurrentStrategy();
                strategy?.UpdateLatency(_server, latency);
                _tcprelay.UpdateLatency(_server, latency);

                OnServerConnected(session);
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
                Close();
            }
        }

        protected void StartPipe(AsyncSession session)
        {
            if (Closed) return;
            try
            {
                _startReceivingTime = DateTime.Now;
                session.Remote.BeginReceive(RemoteRecvBuffer, 0, RecvSize, SocketFlags.None, PipeRemoteReceiveCallback, session);
                Connection.BeginReceive(ConnetionRecvBuffer, 0, RecvSize, SocketFlags.None, PipeConnectionReceiveCallback, session);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void PipeRemoteReceiveCallback(IAsyncResult ar)
        {
            if (Closed) return;
            try
            {
                var session = (AsyncSession)ar.AsyncState;
                int bytesRead = session.Remote.EndReceive(ar);
                _totalRead += bytesRead;
                _tcprelay.UpdateInboundCounter(_server, bytesRead);
                if (bytesRead > 0)
                {
                    lastActivity = DateTime.Now;
                    int bytesToSend;
                    lock (_decryptionLock)
                    {
                        _encryptor.Decrypt(RemoteRecvBuffer, bytesRead, _remoteSendBuffer, out bytesToSend);
                    }
                    Connection.BeginSend(_remoteSendBuffer, 0, bytesToSend, SocketFlags.None, PipeConnectionSendCallback, session);
                    IStrategy strategy = _controller.GetCurrentStrategy();
                    strategy?.UpdateLastRead(_server);
                }
                else
                {
                    Connection.Shutdown(SocketShutdown.Send);
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
            if (Closed) return;
            try
            {
                int bytesRead = Connection.EndReceive(ar);

                var session = (AsyncSession)ar.AsyncState;
                var remote = session.Remote;

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
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void SendToServer(int length, AsyncSession session)
        {
            BeginSendToServer(length, session, PipeRemoteSendCallback);
        }

        private bool _serverHeaderSent;

        protected void BeginSendToServer(int length, AsyncSession session, AsyncCallback callback)
        {
            if (!_serverHeaderSent)
            {
                _serverHeaderSent = true;

                // Append socks5 header
                int len = Socks5Util.HeaderAddrLength(_destEndPoint);
                Array.Copy(ConnetionRecvBuffer, 0, ConnetionRecvBuffer, len, length);
                Socks5Util.FillHeaderAddr(ConnetionRecvBuffer, 0, _destEndPoint);

                length += len;
            }

            _totalWrite += length;
            int bytesToSend;
            lock (_encryptionLock)
            {
                _encryptor.Encrypt(ConnetionRecvBuffer, length, _connetionSendBuffer, out bytesToSend);
            }
            _tcprelay.UpdateOutboundCounter(_server, bytesToSend);
            _startSendingTime = DateTime.Now;
            session.Remote.BeginSend(_connetionSendBuffer, 0, bytesToSend, SocketFlags.None, callback, session);
            IStrategy strategy = _controller.GetCurrentStrategy();
            strategy?.UpdateLastWrite(_server);
        }

        protected AsyncSession EndSendToServer(IAsyncResult ar)
        {
            var session = (AsyncSession)ar.AsyncState;
            session.Remote.EndSend(ar);

            return session;
        }

        private void PipeRemoteSendCallback(IAsyncResult ar)
        {
            if (Closed) return;
            try
            {
                var session = EndSendToServer(ar);
                Connection.BeginReceive(ConnetionRecvBuffer, 0, RecvSize, SocketFlags.None, PipeConnectionReceiveCallback, session);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void PipeConnectionSendCallback(IAsyncResult ar)
        {
            try
            {
                var session = (AsyncSession)ar.AsyncState;
                Connection.EndSend(ar);
                session.Remote.BeginReceive(RemoteRecvBuffer, 0, RecvSize, SocketFlags.None, PipeRemoteReceiveCallback, session);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }
    }
}
