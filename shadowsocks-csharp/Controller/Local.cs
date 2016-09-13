using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Encryption;
using Shadowsocks.Obfs;
using Shadowsocks.Model;
using System.Timers;
using System.Threading;
using OpenDNS;

namespace Shadowsocks.Controller
{
    class CallbackStatus
    {
        protected int status;

        public CallbackStatus()
        {
            status = 0;
        }

        public void SetIfEqu(int newStatus, int oldStatus)
        {
            lock (this)
            {
                if (status == oldStatus)
                {
                    status = newStatus;
                }
            }
        }

        public int Status
        {
            get
            {
                lock (this)
                {
                    return status;
                }
            }
            set
            {
                lock(this)
                {
                    status = value;
                }
            }
        }
    }

    class Local : Listener.Service
    {
        private Configuration _config;
        private ServerTransferTotal _transfer;
        public Local(Configuration config, ServerTransferTotal transfer)
        {
            this._config = config;
            this._transfer = transfer;
        }

        protected bool Accept(byte[] firstPacket, int length)
        {
            if (length < 2)
            {
                return false;
            }
            if (firstPacket[0] == 5 || firstPacket[0] == 4)
            {
                return true;
            }
            if (false && length > 8
                && firstPacket[0] == 'C'
                && firstPacket[1] == 'O'
                && firstPacket[2] == 'N'
                && firstPacket[3] == 'N'
                && firstPacket[4] == 'E'
                && firstPacket[5] == 'C'
                && firstPacket[6] == 'T'
                && firstPacket[7] == ' ')
            {
                return true;
            }
            return false;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket)
        {
            int local_port = ((IPEndPoint)socket.LocalEndPoint).Port;
            if (!_config.GetPortMapCache().ContainsKey(local_port) && !Accept(firstPacket, length))
            {
                return false;
            }
            new ProxyAuthHandler(_config, _transfer, firstPacket, length, socket);
            return true;
        }
    }

    class HandlerConfig
    {
        public string targetHost;
        public int targetPort;

        public Double TTL = 0; // Second
        public Double connect_timeout = 0;
        public int try_keep_alive = 0;
        public string dns_servers;
        public bool fouce_local_dns_query = false;
        // Server proxy
        public int proxyType = 0;
        public string socks5RemoteHost;
        public int socks5RemotePort = 0;
        public string socks5RemoteUsername;
        public string socks5RemotePassword;
        public string proxyUserAgent;
        // auto ban
        public bool autoSwitchOff = true;
        // Reconnect
        public int reconnectTimesRemain = 0;
        public int reconnectTimes = 0;
        public bool forceRandom = false;
    }

    class Handler
    {
        private delegate IPHostEntry GetHostEntryHandler(string ip);

        public delegate Server GetCurrentServer(string targetURI = null, bool usingRandom = false, bool forceRandom = false);
        public delegate void KeepCurrentServer(string targetURI, string id);
        public GetCurrentServer getCurrentServer;
        public KeepCurrentServer keepCurrentServer;
        public Server server;
        public Server select_server;
        public HandlerConfig cfg = new HandlerConfig();
        // Connection socket
        public Socket connection;
        public Socket connectionUDP;
        protected IPEndPoint connectionUDPEndPoint;

        protected ProtocolResponseDetector detector = new ProtocolResponseDetector();
        // remote socket.
        //protected Socket remote;
        protected ProxySocket remote;
        protected ProxySocket remoteUDP;
        protected IPEndPoint remoteUDPEndPoint;
        protected DnsQuery dns;
        // Size of receive buffer.
        protected const int RecvSize = 16384;
        protected const int BufferSize = RecvSize + 1024;
        protected const int AutoSwitchOffErrorTimes = 5;
        // remote header send buffer
        protected byte[] remoteHeaderSendBuffer;
        // connection send buffer
        protected List<byte[]> connectionSendBufferList = new List<byte[]>();

        protected DateTime lastKeepTime;

        protected byte[] remoteUDPRecvBuffer = new byte[RecvSize * 2];
        protected int remoteUDPRecvBufferLength = 0;
        protected object recvUDPoverTCPLock = new object();

        protected bool closed = false;

        protected bool connectionTCPIdle;
        protected bool connectionUDPIdle;
        protected bool remoteTCPIdle;
        protected bool remoteUDPIdle;
        protected int remoteRecvCount = 0;
        protected int connectionRecvCount = 0;

        protected SpeedTester speedTester = new SpeedTester();
        protected int lastErrCode;
        protected Random random = new Random();
        protected System.Timers.Timer timer;
        protected object timerLock = new object();

        enum ConnectState
        {
            END = -1,
            READY = 0,
            HANDSHAKE = 1,
            CONNECTING = 2,
            CONNECTED = 3,
        }
        private ConnectState state = ConnectState.READY;

        private ConnectState State
        {
            get
            {
                return this.state;
            }
            set
            {
                lock (this)
                {
                    this.state = value;
                }
            }
        }

        private void ResetTimeout(Double time)
        {
            if (time <= 0 && timer == null)
                return;

            cfg.try_keep_alive = 0;
            lock (timerLock)
            {
                if (time <= 0)
                {
                    if (timer != null)
                    {
                        timer.Enabled = false;
                        timer.Elapsed -= timer_Elapsed;
                        timer.Dispose();
                        timer = null;
                    }
                }
                else
                {
                    if (timer == null)
                    {
                        timer = new System.Timers.Timer(time * 1000.0);
                        timer.Elapsed += timer_Elapsed;
                    }
                    else
                    {
                        timer.Interval = time * 1000.0;
                        timer.Stop();
                    }
                    timer.Start();
                }
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (closed)
            {
                return;
            }
            bool stop = false;

            try
            {
                if (cfg.try_keep_alive <= 0 && State == ConnectState.CONNECTED && remote != null && remote.CanSendKeepAlive)
                {
                    cfg.try_keep_alive++;
                    RemoteSend(null, -1);
                }
                else
                {
                    if (connection != null)
                    {
                        if (remote != null && cfg.reconnectTimesRemain > 0
                            //&& obfs != null && obfs.getSentLength() == 0
                            && connectionSendBufferList != null
                            && (State == ConnectState.CONNECTED || State == ConnectState.CONNECTING))
                        {
                            remote.Shutdown(SocketShutdown.Both);
                        }
                        else
                        {
                            Server s = server;
                            if (s != null
                                && connectionSendBufferList != null
                                )
                            {
                                if (lastErrCode == 0)
                                {
                                    lastErrCode = 8;
                                    s.ServerSpeedLog().AddTimeoutTimes();
                                }
                            }
                            connection.Shutdown(SocketShutdown.Both);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //
            }
            if (stop)
            {
                Close();
            }
        }

        public void setServerTransferTotal(ServerTransferTotal transfer)
        {
            speedTester.transfer = transfer;
        }

        public int LogSocketException(Exception e)
        {
            // just log useful exceptions, not all of them
            Server s = server;
            if (e is ObfsException)
            {
                ObfsException oe = (ObfsException)e;
                if (lastErrCode == 0)
                {
                    if (s != null)
                    {
                        lastErrCode = 16;
                        s.ServerSpeedLog().AddErrorDecodeTimes();
                        if (s.ServerSpeedLog().ErrorEncryptTimes >= 2
                            && s.ServerSpeedLog().ErrorContinurousTimes >= AutoSwitchOffErrorTimes
                            && cfg.autoSwitchOff)
                        {
                            s.setEnable(false);
                        }
                    }
                }
                return 16; // ObfsException(decrypt error)
            }
            else if (e is ProtocolException)
            {
                ProtocolException pe = (ProtocolException)e;
                if (lastErrCode == 0)
                {
                    if (s != null)
                    {
                        lastErrCode = 16;
                        s.ServerSpeedLog().AddErrorDecodeTimes();
                        if (s.ServerSpeedLog().ErrorEncryptTimes >= 2
                            && s.ServerSpeedLog().ErrorContinurousTimes >= AutoSwitchOffErrorTimes
                            && cfg.autoSwitchOff)
                        {
                            s.setEnable(false);
                        }
                    }
                }
                return 16; // ObfsException(decrypt error)
            }
            else if (e is SocketException)
            {
                SocketException se = (SocketException)e;
                if (se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // closed by browser when sending
                    // normally happens when download is canceled or a tab is closed before page is loaded
                }
                else if (se.ErrorCode == 11004)
                {
                    if (lastErrCode == 0)
                    {
                        if (s != null)
                        {
                            lastErrCode = 1;
                            s.ServerSpeedLog().AddErrorTimes();
                            if (s.ServerSpeedLog().ErrorConnectTimes >= 3
                                && s.ServerSpeedLog().ErrorContinurousTimes >= AutoSwitchOffErrorTimes
                                && cfg.autoSwitchOff)
                            {
                                s.setEnable(false);
                            }
                        }
                    }
                    return 1; // proxy DNS error
                }
                else if (se.SocketErrorCode == SocketError.HostNotFound)
                {
                    if (lastErrCode == 0)
                    {
                        if (s != null)
                        {
                            lastErrCode = 2;
                            s.ServerSpeedLog().AddErrorTimes();
                            if (s.ServerSpeedLog().ErrorConnectTimes >= 3 && cfg.autoSwitchOff)
                            {
                                s.setEnable(false);
                            }
                        }
                    }
                    return 2; // ip not exist
                }
                else if (se.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    if (lastErrCode == 0)
                    {
                        if (s != null)
                        {
                            lastErrCode = 1;
                            s.ServerSpeedLog().AddErrorTimes();
                            if (s.ServerSpeedLog().ErrorConnectTimes >= 3
                                && s.ServerSpeedLog().ErrorContinurousTimes >= AutoSwitchOffErrorTimes
                                && cfg.autoSwitchOff)
                            {
                                s.setEnable(false);
                            }
                        }
                    }
                    return 2; // proxy ip/port error
                }
                else if (se.SocketErrorCode == SocketError.NetworkUnreachable)
                {
                    if (lastErrCode == 0)
                    {
                        if (s != null)
                        {
                            lastErrCode = 3;
                            s.ServerSpeedLog().AddErrorTimes();
                            if (s.ServerSpeedLog().ErrorConnectTimes >= 3
                                && s.ServerSpeedLog().ErrorContinurousTimes >= AutoSwitchOffErrorTimes
                                && cfg.autoSwitchOff)
                            {
                                s.setEnable(false);
                            }
                        }
                    }
                    return 3; // proxy ip/port error
                }
                else if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    if (lastErrCode == 0)
                    {
                        if (s != null)
                        {
                            lastErrCode = 8;
                            s.ServerSpeedLog().AddTimeoutTimes();
                        }
                    }
                    return 8; // proxy server no response too slow
                }
                else
                {
                    if (lastErrCode == 0)
                    {
                        lastErrCode = -1;
                        if (s != null)
                            s.ServerSpeedLog().AddNoErrorTimes(); //?
                    }
                    return 0;
                }
            }
            return 0;
        }

        public bool ReConnect()
        {
            System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString() + "]" + "Reconnect " + cfg.targetHost + ":" + cfg.targetPort.ToString() + " " + connection.Handle.ToString());
            {
                Handler handler = new Handler();
                handler.getCurrentServer = getCurrentServer;
                handler.keepCurrentServer = keepCurrentServer;
                handler.select_server = select_server;
                handler.connection = connection;
                handler.connectionUDP = connectionUDP;
                handler.cfg = cfg;
                handler.cfg.reconnectTimesRemain = cfg.reconnectTimesRemain - 1;
                handler.cfg.reconnectTimes = cfg.reconnectTimes + 1;

                handler.speedTester.transfer = speedTester.transfer;

                int total_len = 0;
                byte[] newFirstPacket = remoteHeaderSendBuffer;
                if (connectionSendBufferList != null && connectionSendBufferList.Count > 0)
                {
                    foreach (byte[] data in connectionSendBufferList)
                    {
                        total_len += data.Length;
                    }
                    newFirstPacket = new byte[total_len];
                    total_len = 0;
                    foreach (byte[] data in connectionSendBufferList)
                    {
                        Buffer.BlockCopy(data, 0, newFirstPacket, total_len, data.Length);
                        total_len += data.Length;
                    }
                }
                handler.Start(newFirstPacket, newFirstPacket.Length);
            }
            return true;
        }

        public void Start(byte[] firstPacket, int length)
        {
            if (cfg.socks5RemotePort > 0)
            {
                cfg.autoSwitchOff = false;
            }
            ResetTimeout(cfg.TTL);
            if (this.State == ConnectState.READY)
            {
                State = ConnectState.HANDSHAKE;
                remoteHeaderSendBuffer = firstPacket;
                Connect();
            }
        }

        private void BeginConnect(IPAddress ipAddress, int serverPort)
        {
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);
            if (server.server_udp_port == 0)
            {
                IPEndPoint _remoteEP = new IPEndPoint(ipAddress, serverPort);
                remoteUDPEndPoint = remoteEP;
            }
            else
            {
                IPEndPoint _remoteEP = new IPEndPoint(ipAddress, server.server_udp_port);
                remoteUDPEndPoint = _remoteEP;
            }

            if (cfg.socks5RemotePort != 0
                || connectionUDP == null
                || connectionUDP != null && server.udp_over_tcp)
            {
                remote = new ProxySocket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                remote.GetSocket().SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                try
                {
                    remote.SetEncryptor(EncryptorFactory.GetEncryptor(server.method, server.password));
                }
                catch
                {

                }
                remote.SetProtocol(ObfsFactory.GetObfs(server.protocol));
                remote.SetObfs(ObfsFactory.GetObfs(server.obfs));
            }

            if (connectionUDP != null && !server.udp_over_tcp)
            {
                try
                {
                    remoteUDP = new ProxySocket(ipAddress.AddressFamily,
                        SocketType.Dgram, ProtocolType.Udp);
                    remoteUDP.GetSocket().Bind(new IPEndPoint(ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0));

                    remoteUDP.SetEncryptor(EncryptorFactory.GetEncryptor(server.method, server.password));
                    remoteUDP.SetProtocol(ObfsFactory.GetObfs(server.protocol));
                    remoteUDP.SetObfs(ObfsFactory.GetObfs(server.obfs));
                }
                catch (SocketException)
                {
                    remoteUDP = null;
                }
            }
            ResetTimeout(cfg.TTL);

            // Connect to the remote endpoint.
            if (cfg.socks5RemotePort == 0 && connectionUDP != null && !server.udp_over_tcp)
            {
                ConnectState _state = this.State;
                if (_state == ConnectState.CONNECTING)
                {
                    StartPipe();
                }
            }
            else
            {
                speedTester.BeginConnect();
                IAsyncResult result = remote.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), new CallbackStatus());
                double t = cfg.connect_timeout <= 0 ? 30 : cfg.connect_timeout;
                bool success = result.AsyncWaitHandle.WaitOne((int)(t * 1000), true);
                if (!success)
                {
                    ((CallbackStatus)result.AsyncState).SetIfEqu(-1, 0);
                    if (((CallbackStatus)result.AsyncState).Status == -1)
                    {
                        if (lastErrCode == 0)
                        {
                            lastErrCode = 8;
                            server.ServerSpeedLog().AddTimeoutTimes();
                        }
                        CloseSocket(ref remote);
                        Close();
                    }
                }
            }
        }

        public bool TryReconnect()
        {
            if (cfg.reconnectTimesRemain > 0)
            {
                if (this.State == ConnectState.CONNECTING)
                {
                    return this.ReConnect();
                }
                //else if (this.State == ConnectState.CONNECTED)
                //{
                //    if (connectionSendBufferList != null)
                //    {
                //        //this.State = ConnectState.CONNECTING;
                //        return this.ReConnect();
                //    }
                //}
            }
            return false;
        }

        private void CloseSocket(ref Socket sock)
        {
            lock (this)
            {
                if (sock != null)
                {
                    Socket s = sock;
                    sock = null;
                    try
                    {
                        s.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                    try
                    {
                        s.Close();
                    }
                    catch { }
                }
            }
        }

        private void CloseSocket(ref ProxySocket sock)
        {
            lock (this)
            {
                if (sock != null)
                {
                    ProxySocket s = sock;
                    sock = null;
                    try
                    {
                        s.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                    try
                    {
                        s.Close();
                    }
                    catch { }
                }
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (closed)
                {
                    return;
                }
                closed = true;
            }
            for (int i = 0; i < 10; ++i)
            {
                if (remoteRecvCount <= 0 && connectionRecvCount <= 0)
                    break;
                Thread.Sleep(10 * (i + 1) * (i + 1));
            }
            {
                CloseSocket(ref remote);
                CloseSocket(ref remoteUDP);
            }
            System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString() + "]" + "Close   " + cfg.targetHost + ":" + cfg.targetPort.ToString() + " " + connection.Handle.ToString());
            if (lastErrCode == 0 && server != null)
            {
                if (speedTester.sizeRecv == 0 && speedTester.sizeUpload > 0)
                    server.ServerSpeedLog().AddErrorEmptyTimes();
                else
                    server.ServerSpeedLog().AddNoErrorTimes();
            }
            keepCurrentServer(cfg.targetHost, server.id);
            ResetTimeout(0);
            try
            {
                bool reconnect = TryReconnect();
                //lock (this)
                {
                    if (this.State != ConnectState.END)
                    {
                        if (this.State != ConnectState.READY && this.State != ConnectState.HANDSHAKE && server != null)
                        {
                            server.ServerSpeedLog().AddDisconnectTimes();
                            server.GetConnections().DecRef(this.connection);
                        }
                        this.State = ConnectState.END;
                    }
                    getCurrentServer = null;
                    keepCurrentServer = null;
                }

                if (!reconnect)
                {
                    System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString() + "]" + "Disconnect " + cfg.targetHost + ":" + cfg.targetPort.ToString() + " " + connection.Handle.ToString());
                    CloseSocket(ref connection);
                    CloseSocket(ref connectionUDP);
                }
                else
                {
                    connection = null;
                    connectionUDP = null;
                }

                getCurrentServer = null;
                keepCurrentServer = null;

                detector = null;
                speedTester = null;
                random = null;
                remoteUDPRecvBuffer = null;

                server = null;
                select_server = null;
                cfg = null;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private bool ConnectProxyServer(string strRemoteHost, int iRemotePort)
        {
            if (cfg.proxyType == 0)
            {
                bool ret = remote.ConnectSocks5ProxyServer(strRemoteHost, iRemotePort, connectionUDP != null && !server.udp_over_tcp, cfg.socks5RemoteUsername, cfg.socks5RemotePassword);
                remoteUDPEndPoint = remote.GetProxyUdpEndPoint();
                remote.SetTcpServer(server.server, server.server_port);
                remote.SetUdpServer(server.server, server.server_udp_port == 0 ? server.server_port : server.server_udp_port);
                if (remoteUDP != null)
                {
                    remoteUDP.GoS5Proxy = true;
                    remoteUDP.SetUdpServer(server.server, server.server_udp_port == 0 ? server.server_port : server.server_udp_port);
                }
                return ret;
            }
            else if (cfg.proxyType == 1)
            {
                bool ret = remote.ConnectHttpProxyServer(strRemoteHost, iRemotePort, cfg.socks5RemoteUsername, cfg.socks5RemotePassword, cfg.proxyUserAgent);
                remote.SetTcpServer(server.server, server.server_port);
                return ret;
            }
            else
            {
                return true;
            }
        }

        private IPAddress QueryDns(string host, string dns_servers)
        {
            IPAddress ipAddress;
            bool parsed = IPAddress.TryParse(host, out ipAddress);
            if (!parsed)
            {
                if (server.DnsBuffer().isExpired(host))
                {
                    if (dns_servers != null)
                    {
                        OpenDNS.Types[] types;
                        //if (false)
                        //    types = new Types[] { Types.AAAA, Types.A };
                        //else
                            types = new Types[] { Types.A, Types.AAAA };
                        string[] dns_server = dns_servers.Split(',');
                        for (int query_i = 0; query_i < types.Length; ++query_i)
                        {
                            dns = new DnsQuery(host, types[query_i]);
                            dns.RecursionDesired = true;
                            foreach (string server in dns_server)
                            {
                                dns.Servers.Add(server);
                            }
                            if (dns.Send())
                            {
                                int count = dns.Response.Answers.Count;
                                if (count > 0)
                                {
                                    for (int i = 0; i < count; ++i)
                                    {
                                        if (((ResourceRecord)dns.Response.Answers[i]).Type != types[query_i])
                                            continue;
                                        return ((OpenDNS.Address)dns.Response.Answers[i]).IP;
                                    }
                                }
                            }
                        }
                    }
                    {
                        try
                        {
                            GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
                            IAsyncResult result = callback.BeginInvoke(host, null, null);
                            if (result.AsyncWaitHandle.WaitOne(5, false))
                            {
                                foreach(IPAddress ad in callback.EndInvoke(result).AddressList)
                                {
                                    return ad;
                                    //if (ad.AddressFamily == AddressFamily.InterNetwork)
                                    //{
                                    //    return ad;
                                    //}
                                }
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            return ipAddress;
        }

        private void Connect()
        {
            remote = null;
            remoteUDP = null;
            if (select_server == null)
            {
                if (cfg.targetHost == null)
                {
                    cfg.targetHost = GetQueryString();
                    cfg.targetPort = GetQueryPort();
                    server = this.getCurrentServer(cfg.targetHost, true);
                }
                else
                {
                    server = this.getCurrentServer(cfg.targetHost, true, cfg.forceRandom);
                }
            }
            else
            {
                server = select_server;
            }
            speedTester.server = server.server;
            System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString() + "]" + "Connect " + cfg.targetHost + ":" + cfg.targetPort.ToString() + " " + connection.Handle.ToString());

            ResetTimeout(cfg.TTL);
            if (cfg.fouce_local_dns_query && cfg.targetHost != null)
            {
                IPAddress ipAddress = QueryDns(cfg.targetHost, cfg.dns_servers);
                if (ipAddress != null)
                {
                    server.DnsBuffer().UpdateDns(cfg.targetHost, ipAddress);
                    cfg.targetHost = ipAddress.ToString();
                    ResetTimeout(cfg.TTL);
                }
            }

            lock (this)
            {
                server.ServerSpeedLog().AddConnectTimes();
                if (this.State == ConnectState.HANDSHAKE)
                {
                    this.State = ConnectState.CONNECTING;
                }
                server.GetConnections().AddRef(this.connection);
            }
            {
                IPAddress ipAddress;
                string serverURI = server.server;
                int serverPort = server.server_port;
                if (cfg.socks5RemotePort > 0)
                {
                    serverURI = cfg.socks5RemoteHost;
                    serverPort = cfg.socks5RemotePort;
                }
                bool parsed = IPAddress.TryParse(serverURI, out ipAddress);
                if (!parsed)
                {
                    if (server.DnsBuffer().isExpired(serverURI))
                    {
                        bool dns_ok = false;
                        if (!dns_ok)
                        {
                            ipAddress = QueryDns(serverURI, cfg.dns_servers);
                            if (ipAddress != null)
                            {
                                server.DnsBuffer().UpdateDns(serverURI, ipAddress);
                                dns_ok = true;
                            }
                        }
                        if (!dns_ok)
                        {
                            lastErrCode = 8;
                            server.ServerSpeedLog().AddTimeoutTimes();
                            Close();
                            return;
                        }
                    }
                    else
                    {
                        ipAddress = server.DnsBuffer().ip;
                    }
                }
                BeginConnect(ipAddress, serverPort);
            }
        }


        private void ConnectCallback(IAsyncResult ar)
        {
            if (ar != null && ar.AsyncState != null)
            {
                ((CallbackStatus)ar.AsyncState).SetIfEqu(1, 0);
                if (((CallbackStatus)ar.AsyncState).Status != 1)
                    return;
            }
            try
            {
                remote.EndConnect(ar);
                if (cfg.socks5RemotePort > 0)
                {
                    if (!ConnectProxyServer(server.server, server.server_port))
                    {
                        throw new SocketException((int)SocketError.ConnectionReset);
                    }
                }
                speedTester.EndConnect();

                ConnectState _state = this.State;
                if (_state == ConnectState.CONNECTING)
                {
                    StartPipe();
                }
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        // do/end xxx tcp/udp Recv
        private void doConnectionTCPRecv()
        {
            if (connection != null && connectionTCPIdle)
            {
                connectionTCPIdle = false;
                byte[] buffer = new byte[RecvSize * 2];
                connection.BeginReceive(buffer, 0, RecvSize, 0,
                    new AsyncCallback(PipeConnectionReceiveCallback), buffer);
            }
        }

        private int endConnectionTCPRecv(IAsyncResult ar)
        {
            if (connection != null)
            {
                int bytesRead = connection.EndReceive(ar);
                connectionTCPIdle = true;
                return bytesRead;
            }
            return 0;
        }

        private void doConnectionUDPRecv()
        {
            if (connectionUDP != null && connectionUDPIdle)
            {
                connectionUDPIdle = false;
                const int RecvSize = 65536;
                IPEndPoint sender = new IPEndPoint(connectionUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                EndPoint tempEP = (EndPoint)sender;
                byte[] buffer = new byte[RecvSize];
                connectionUDP.BeginReceiveFrom(buffer, 0, RecvSize, SocketFlags.None, ref tempEP,
                    new AsyncCallback(PipeConnectionUDPReceiveCallback), buffer);
            }
        }

        private int endConnectionUDPRecv(IAsyncResult ar, ref EndPoint endPoint)
        {
            if (connectionUDP != null)
            {
                int bytesRead = connectionUDP.EndReceiveFrom(ar, ref endPoint);
                if (connectionUDPEndPoint == null)
                    connectionUDPEndPoint = (IPEndPoint)endPoint;
                connectionUDPIdle = true;
                return bytesRead;
            }
            return 0;
        }

        private void doRemoteTCPRecv()
        {
            if (remote != null && remoteTCPIdle)
            {
                remoteTCPIdle = false;
                remote.BeginReceive(new byte[RecvSize * 2], RecvSize, 0,
                    new AsyncCallback(PipeRemoteReceiveCallback), null);
            }
        }

        private int endRemoteTCPRecv(IAsyncResult ar)
        {
            if (remote != null)
            {
                bool sendback;
                int bytesRead = remote.EndReceive(ar, out sendback);
                if (sendback)
                {
                    RemoteSendback(new byte[0], 0);
                }
                remoteTCPIdle = true;
                return bytesRead;
            }
            return 0;
        }

        private void doRemoteUDPRecv()
        {
            if (remoteUDP != null && remoteUDPIdle)
            {
                remoteUDPIdle = false;
                const int RecvSize = 65536;
                IPEndPoint sender = new IPEndPoint(remoteUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                EndPoint tempEP = (EndPoint)sender;
                remoteUDP.BeginReceiveFrom(new byte[RecvSize], RecvSize, SocketFlags.None, ref tempEP,
                    new AsyncCallback(PipeRemoteUDPReceiveCallback), null);
            }
        }

        private int endRemoteUDPRecv(IAsyncResult ar, ref EndPoint endPoint)
        {
            if (remoteUDP != null)
            {
                int bytesRead = remoteUDP.EndReceiveFrom(ar, ref endPoint);
                remoteUDPIdle = true;
                return bytesRead;
            }
            return 0;
        }

        private void doConnectionRecv()
        {
            doConnectionTCPRecv();
            doConnectionUDPRecv();
        }

        private void doRemoteRecv()
        {
            doRemoteTCPRecv();
            doRemoteUDPRecv();
        }

        private void SetObfsPlugin()
        {
            int head_len = 30;
            if (connectionSendBufferList != null && connectionSendBufferList.Count > 0)
            {
                head_len = ObfsBase.GetHeadSize(connectionSendBufferList[0], 30);
            }
            else
            {
                head_len = ObfsBase.GetHeadSize(remoteHeaderSendBuffer, 30);
            }
            remote?.SetObfsPlugin(server, head_len);
            remoteUDP?.SetObfsPlugin(server, head_len);
        }

        private string GetQueryString()
        {
            if (remoteHeaderSendBuffer == null)
                return null;

            if (remoteHeaderSendBuffer[0] == 1)
            {
                if (remoteHeaderSendBuffer.Length > 4)
                {
                    byte[] addr = new byte[4];
                    Array.Copy(remoteHeaderSendBuffer, 1, addr, 0, 4);
                    IPAddress ipAddress = new IPAddress(addr);
                    return ipAddress.ToString();
                }
                return null;
            }
            if (remoteHeaderSendBuffer[0] == 4)
            {
                if (remoteHeaderSendBuffer.Length > 16)
                {
                    byte[] addr = new byte[16];
                    Array.Copy(remoteHeaderSendBuffer, 1, addr, 0, 16);
                    IPAddress ipAddress = new IPAddress(addr);
                    return ipAddress.ToString();
                }
                return null;
            }
            if (remoteHeaderSendBuffer[0] == 3 && remoteHeaderSendBuffer.Length > 1)
            {
                if (remoteHeaderSendBuffer.Length > remoteHeaderSendBuffer[1] + 1)
                {
                    string url = System.Text.Encoding.UTF8.GetString(remoteHeaderSendBuffer, 2, remoteHeaderSendBuffer[1]);
                    return url;
                }
            }
            return null;
        }

        private int GetQueryPort()
        {
            if (remoteHeaderSendBuffer == null)
                return 0;

            if (remoteHeaderSendBuffer[0] == 1)
            {
                if (remoteHeaderSendBuffer.Length > 6)
                {
                    int port = (remoteHeaderSendBuffer[5] << 8) | remoteHeaderSendBuffer[6];
                    return port;
                }
                return 0;
            }
            if (remoteHeaderSendBuffer[0] == 4)
            {
                if (remoteHeaderSendBuffer.Length > 18)
                {
                    int port = (remoteHeaderSendBuffer[17] << 8) | remoteHeaderSendBuffer[18];
                    return port;
                }
                return 0;
            }
            if (remoteHeaderSendBuffer[0] == 3 && remoteHeaderSendBuffer.Length > 1)
            {
                if (remoteHeaderSendBuffer.Length > remoteHeaderSendBuffer[1] + 1)
                {
                    int port = (remoteHeaderSendBuffer[remoteHeaderSendBuffer[1] + 2] << 8) | remoteHeaderSendBuffer[remoteHeaderSendBuffer[1] + 3];
                    return port;
                }
            }
            return 0;
        }

        // 2 sides connection start
        private void StartPipe()
        {
            try
            {
                // set mark
                connectionTCPIdle = true;
                connectionUDPIdle = true;
                remoteTCPIdle = true;
                remoteUDPIdle = true;
                closed = false;

                remoteUDPRecvBufferLength = 0;
                SetObfsPlugin();

                ResetTimeout(cfg.TTL);

                speedTester.BeginUpload();

                // remote ready
                if (connectionUDP == null) // TCP
                {
                    detector.OnSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                    byte[] data = new byte[remoteHeaderSendBuffer.Length];
                    Array.Copy(remoteHeaderSendBuffer, data, data.Length);
                    connectionSendBufferList.Add(data);
                    RemoteSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                    remoteHeaderSendBuffer = null;
                    //remote.GetSocket().SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, false);
                }
                else // UDP
                {
                    if (!server.udp_over_tcp &&
                        remoteUDP != null)
                    {
                        if (cfg.socks5RemotePort == 0)
                            CloseSocket(ref remote);
                        remoteHeaderSendBuffer = null;
                    }
                    else if (remoteHeaderSendBuffer != null)
                    {
                        RemoteSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                        remoteHeaderSendBuffer = null;
                    }
                }
                this.State = ConnectState.CONNECTED;

                // remote recv first
                doRemoteTCPRecv();
                doRemoteUDPRecv();

                doConnectionTCPRecv();
                doConnectionUDPRecv();
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        private void ConnectionSend(byte[] buffer, int bytesToSend)
        {
            if (connectionUDP == null)
                connection.BeginSend(buffer, 0, bytesToSend, 0, new AsyncCallback(PipeConnectionSendCallback), null);
            else
                connectionUDP.BeginSendTo(buffer, 0, bytesToSend, SocketFlags.None, connectionUDPEndPoint, new AsyncCallback(PipeConnectionUDPSendCallback), null);
        }

        private void UDPoverTCPConnectionSend(byte[] send_buffer, int bytesToSend)
        {
            List<byte[]> buffer_list = new List<byte[]>();
            lock (recvUDPoverTCPLock)
            {
                if (bytesToSend + remoteUDPRecvBufferLength > remoteUDPRecvBuffer.Length)
                {
                    Array.Resize(ref remoteUDPRecvBuffer, bytesToSend + remoteUDPRecvBufferLength);
                }
                Array.Copy(send_buffer, 0, remoteUDPRecvBuffer, remoteUDPRecvBufferLength, bytesToSend);
                remoteUDPRecvBufferLength += bytesToSend;
                while (remoteUDPRecvBufferLength > 6)
                {
                    int len = (remoteUDPRecvBuffer[0] << 8) + remoteUDPRecvBuffer[1];
                    if (len >= 0xff00)
                    {
                        len = (remoteUDPRecvBuffer[1] << 8) + remoteUDPRecvBuffer[2] + 0xff00;
                    }
                    if (len > remoteUDPRecvBufferLength)
                        break;
                    if (len >= 0xff00)
                    {
                        byte[] buffer = new byte[len - 1];
                        Array.Copy(remoteUDPRecvBuffer, 1, buffer, 0, len - 1);
                        remoteUDPRecvBufferLength -= len;
                        Array.Copy(remoteUDPRecvBuffer, len, remoteUDPRecvBuffer, 0, remoteUDPRecvBufferLength);

                        buffer[0] = 0;
                        buffer[1] = 0;
                        buffer_list.Add(buffer);
                    }
                    else
                    {
                        byte[] buffer = new byte[len];
                        Array.Copy(remoteUDPRecvBuffer, buffer, len);
                        remoteUDPRecvBufferLength -= len;
                        Array.Copy(remoteUDPRecvBuffer, len, remoteUDPRecvBuffer, 0, remoteUDPRecvBufferLength);

                        buffer[0] = 0;
                        buffer[1] = 0;
                        buffer_list.Add(buffer);
                    }
                }
            }
            if (buffer_list.Count == 0)
            {
                doRemoteTCPRecv();
            }
            else
            {
                foreach (byte[] buffer in buffer_list)
                {
                    if (buffer == buffer_list[buffer_list.Count - 1])
                        connectionUDP.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, connectionUDPEndPoint, new AsyncCallback(PipeConnectionUDPSendCallback), null);
                    else
                        connectionUDP.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, connectionUDPEndPoint, new AsyncCallback(PipeConnectionUDPSendCallbackNoRecv), null);
                }
            }
        }

        private void PipeRemoteReceiveCallback(IAsyncResult ar)
        {
            Interlocked.Increment(ref remoteRecvCount);
            bool final_close = false;
            try
            {
                if (closed)
                {
                    return;
                }
                int bytesRead = endRemoteTCPRecv(ar);

                if (remote.IsClose)
                {
                    final_close = true;
                }
                else
                {
                    int bytesRecv = remote.GetAsyncResultSize(ar);
                    if (speedTester.BeginDownload())
                    {
                        int pingTime = -1;
                        if (speedTester.timeBeginDownload != null && speedTester.timeBeginUpload != null)
                            pingTime = (int)(speedTester.timeBeginDownload - speedTester.timeBeginUpload).TotalMilliseconds;
                        if (pingTime >= 0)
                            server.ServerSpeedLog().AddConnectTime(pingTime);
                    }
                    server.ServerSpeedLog().AddDownloadBytes(bytesRecv, DateTime.Now);
                    speedTester.AddDownloadSize(bytesRecv);
                    ResetTimeout(cfg.TTL);

                    if (bytesRead <= 0)
                    {
                        doRemoteTCPRecv();
                    }
                    else //if (bytesRead > 0)
                    {
                        byte[] remoteSendBuffer = new byte[BufferSize * 2];

                        Array.Copy(remote.GetAsyncResultBuffer(ar), remoteSendBuffer, bytesRead);
                        if (connectionUDP == null)
                        {
                            if (detector.OnRecv(remoteSendBuffer, bytesRead) > 0)
                            {
                                server.ServerSpeedLog().AddErrorTimes();
                            }
                            if (detector.Pass)
                            {
                                server.ServerSpeedLog().ResetErrorDecodeTimes();
                            }
                            else
                            {
                                server.ServerSpeedLog().ResetEmptyTimes();
                            }
                            connection.BeginSend(remoteSendBuffer, 0, bytesRead, 0, new AsyncCallback(PipeConnectionSendCallback), null);
                        }
                        else
                        {
                            UDPoverTCPConnectionSend(remoteSendBuffer, bytesRead);
                        }
                        server.ServerSpeedLog().AddDownloadRawBytes(bytesRead);
                        speedTester.AddRecvSize(bytesRead);
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e);
                final_close = true;
            }
            finally
            {
                Interlocked.Decrement(ref remoteRecvCount);
                if (final_close)
                {
                    Close();
                }
            }
        }

        // end ReceiveCallback
        private void PipeRemoteUDPReceiveCallback(IAsyncResult ar)
        {
            bool final_close = false;
            Interlocked.Decrement(ref remoteRecvCount);
            try
            {
                if (closed)
                {
                    return;
                }
                IPEndPoint sender = new IPEndPoint(remoteUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                EndPoint tempEP = (EndPoint)sender;

                int bytesRead = endRemoteUDPRecv(ar, ref tempEP);

                if (remoteUDP.IsClose)
                {
                    final_close = true;
                }
                else
                {
                    int bytesRecv = remoteUDP.GetAsyncResultSize(ar);
                    if (speedTester.BeginDownload())
                    {
                        int pingTime = -1;
                        if (speedTester.timeBeginDownload != null && speedTester.timeBeginUpload != null)
                            pingTime = (int)(speedTester.timeBeginDownload - speedTester.timeBeginUpload).TotalMilliseconds;
                        if (pingTime >= 0)
                            server.ServerSpeedLog().AddConnectTime(pingTime);
                    }
                    server.ServerSpeedLog().AddDownloadBytes(bytesRecv, DateTime.Now);
                    speedTester.AddDownloadSize(bytesRecv);
                    ResetTimeout(cfg.TTL);

                    if (bytesRead <= 0)
                    {
                        doRemoteUDPRecv();
                    }
                    else //if (bytesRead > 0)
                    {
                        ConnectionSend(remoteUDP.GetAsyncResultBuffer(ar), bytesRead);

                        speedTester.AddRecvSize(bytesRead);
                        server.ServerSpeedLog().AddDownloadRawBytes(bytesRead);
                    }
                }
            }
            catch (Exception e)
            {
                LogException(e);
                final_close = true;
            }
            finally
            {
                Interlocked.Decrement(ref remoteRecvCount);
                if (final_close)
                {
                    Close();
                }
            }
        }

        private void PipeRemoteSendbackCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                remote.EndSend(ar);
                if (lastKeepTime == null || (DateTime.Now - lastKeepTime).TotalSeconds > 5)
                {
                    if (keepCurrentServer != null) keepCurrentServer(cfg.targetHost, server.id);
                    lastKeepTime = DateTime.Now;
                }
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        private void RemoteSendback(byte[] bytes, int length)
        {
            int send_len;
            send_len = remote.BeginSend(bytes, length, SocketFlags.None, new AsyncCallback(PipeRemoteSendbackCallback), null);
            server.ServerSpeedLog().AddUploadBytes(send_len, DateTime.Now);
            speedTester.AddUploadSize(send_len);
        }


        private void RemoteSend(byte[] bytes, int length)
        {
            int send_len;
            send_len = remote.BeginSend(bytes, length, SocketFlags.None, new AsyncCallback(PipeRemoteSendCallback), null);
            server.ServerSpeedLog().AddUploadBytes(send_len, DateTime.Now);
            speedTester.AddUploadSize(send_len);
        }

        private void RemoteSendto(byte[] bytes, int length)
        {
            int send_len;
            send_len = remoteUDP.BeginSendTo(bytes, length, SocketFlags.None, remoteUDPEndPoint, new AsyncCallback(PipeRemoteUDPSendCallback), null);
            server.ServerSpeedLog().AddUploadBytes(send_len, DateTime.Now);
            speedTester.AddUploadSize(send_len);
        }

        private void PipeConnectionReceiveCallback(IAsyncResult ar)
        {
            Interlocked.Increment(ref connectionRecvCount);
            bool final_close = false;
            try
            {
                if (closed)
                {
                    return;
                }
                int bytesRead = endConnectionTCPRecv(ar);

                if (bytesRead > 0)
                {
                    if (connectionUDP != null)
                    {
                        doConnectionTCPRecv();
                        ResetTimeout(cfg.TTL);
                        return;
                    }
                    byte[] connetionRecvBuffer = new byte[RecvSize * 2];
                    Array.Copy((byte[])ar.AsyncState, 0, connetionRecvBuffer, 0, bytesRead);
                    if (State == ConnectState.CONNECTED)
                    {
                        if (remoteHeaderSendBuffer != null)
                        {
                            Array.Copy(connetionRecvBuffer, 0, connetionRecvBuffer, remoteHeaderSendBuffer.Length, bytesRead);
                            Array.Copy(remoteHeaderSendBuffer, 0, connetionRecvBuffer, 0, remoteHeaderSendBuffer.Length);
                            bytesRead += remoteHeaderSendBuffer.Length;
                            remoteHeaderSendBuffer = null;
                        }
                        else
                        {
                            Logging.LogBin(LogLevel.Debug, "remote send", connetionRecvBuffer, bytesRead);
                        }
                    }
                    if (speedTester.sizeRecv > 0)
                    {
                        connectionSendBufferList = null;
                    }
                    else if (connectionSendBufferList != null)
                    {
                        detector.OnSend(connetionRecvBuffer, bytesRead);
                        byte[] data = new byte[bytesRead];
                        Array.Copy(connetionRecvBuffer, data, data.Length);
                        connectionSendBufferList.Add(data);
                    }
                    if (closed || State != ConnectState.CONNECTED)
                    {
                        return;
                    }

                    ResetTimeout(cfg.TTL);
                    RemoteSend(connetionRecvBuffer, bytesRead);
                }
                else
                {
                    final_close = true;
                }
            }
            catch (Exception e)
            {
                LogException(e);
                final_close = true;
            }
            finally
            {
                Interlocked.Decrement(ref connectionRecvCount);
                if (final_close)
                {
                    Close();
                }
            }
        }

        private void PipeConnectionUDPReceiveCallback(IAsyncResult ar)
        {
            bool final_close = false;
            Interlocked.Increment(ref connectionRecvCount);
            try
            {
                if (closed)
                {
                    return;
                }
                IPEndPoint sender = new IPEndPoint(connectionUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                EndPoint tempEP = (EndPoint)sender;

                int bytesRead = endConnectionUDPRecv(ar, ref tempEP);

                if (bytesRead > 0)
                {
                    byte[] connetionSendBuffer = new byte[bytesRead];
                    Array.Copy((byte[])ar.AsyncState, connetionSendBuffer, bytesRead);
                    if (!server.udp_over_tcp && remoteUDP != null)
                    {
                        RemoteSendto(connetionSendBuffer, bytesRead);
                    }
                    else
                    {
                        if (connetionSendBuffer[0] == 0 && connetionSendBuffer[1] == 0)
                        {
                            if (bytesRead >= 0xff00)
                            {
                                byte[] connetionSendBuffer2 = new byte[bytesRead + 1];
                                Array.Copy(connetionSendBuffer, 0, connetionSendBuffer2, 1, bytesRead);
                                connetionSendBuffer2[0] = 0xff;
                                connetionSendBuffer2[1] = (byte)((bytesRead - 0xff00 + 1) >> 8);
                                connetionSendBuffer2[2] = (byte)((bytesRead - 0xff00 + 1));
                                RemoteSend(connetionSendBuffer2, bytesRead + 1);
                            }
                            else
                            {
                                connetionSendBuffer[0] = (byte)(bytesRead >> 8);
                                connetionSendBuffer[1] = (byte)(bytesRead);
                                RemoteSend(connetionSendBuffer, bytesRead);
                            }
                        }
                    }
                    ResetTimeout(cfg.TTL);
                }
                else
                {
                    final_close = true;
                }
            }
            catch (Exception e)
            {
                LogException(e);
                final_close = true;
            }
            finally
            {
                Interlocked.Decrement(ref connectionRecvCount);
                if (final_close)
                {
                    Close();
                }
            }
        }

        private void PipeRemoteSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                remote.EndSend(ar);
                doConnectionRecv();
                if (lastKeepTime == null || (DateTime.Now - lastKeepTime).TotalSeconds > 5)
                {
                    if (keepCurrentServer != null) keepCurrentServer(cfg.targetHost, server.id);
                    lastKeepTime = DateTime.Now;
                }
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        private void PipeRemoteUDPSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                remoteUDP.EndSendTo(ar);
                doConnectionRecv();
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        private void PipeConnectionSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connection.EndSend(ar);
                doRemoteRecv();
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        private void PipeConnectionUDPSendCallbackNoRecv(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connectionUDP.EndSendTo(ar);
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        private void PipeConnectionUDPSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connectionUDP.EndSendTo(ar);
                doRemoteRecv();
            }
            catch (Exception e)
            {
                LogExceptionAndClose(e);
            }
        }

        protected string getServerUrl(out string remarks)
        {
            Server s = server;
            if (s == null)
            {
                remarks = "";
                return "";
            }
            remarks = s.remarks;
            return s.server;
        }

        private void LogException(Exception e)
        {
            LogSocketException(e);
            string remarks;
            string server_url = getServerUrl(out remarks);
            if (!Logging.LogSocketException(remarks, server_url, e))
                Logging.LogUsefulException(e);
        }

        private void LogExceptionAndClose(Exception e)
        {
            LogException(e);
            Close();
        }
    }

}
