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

namespace Shadowsocks.Controller
{
    public class ProtocolException : Exception
    {
        public ProtocolException(string info)
            : base(info)
        {

        }
    }

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
        private static bool EncryptorLoaded = false;
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
            //if (length > 8
            //    && firstPacket[0] == 'C'
            //    && firstPacket[1] == 'O'
            //    && firstPacket[2] == 'N'
            //    && firstPacket[3] == 'N'
            //    && firstPacket[4] == 'E'
            //    && firstPacket[5] == 'C'
            //    && firstPacket[6] == 'T'
            //    && firstPacket[7] == ' ')
            //{
            //    return true;
            //}
            return false;
        }

        bool AuthConnection(Socket connection, string authUser, string authPass)
        {
            if ((_config.authUser ?? "" ).Length == 0)
            {
                return true;
            }
            if (_config.authUser == authUser && (_config.authPass ?? "") == authPass)
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
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            Handler handler = new Handler();

            handler.getCurrentServer = delegate (string targetURI, bool usingRandom, bool forceRandom) { return _config.GetCurrentServer(targetURI, usingRandom, forceRandom); };
            handler.keepCurrentServer = delegate (string targetURI) { _config.KeepCurrentServer(targetURI); };
            handler.connection = socket;
            handler.reconnectTimesRemain = _config.reconnectTimes;
            handler.forceRandom = _config.random;
            handler.setServerTransferTotal(_transfer);

            //handler.server = _config.GetCurrentServer(null, true);
            if (_config.proxyEnable)
            {
                handler.proxyType = _config.proxyType;
                handler.socks5RemoteHost = _config.proxyHost;
                handler.socks5RemotePort = _config.proxyPort;
                handler.socks5RemoteUsername = _config.proxyAuthUser;
                handler.socks5RemotePassword = _config.proxyAuthPass;
            }
            handler.TTL = _config.TTL;
            handler.autoSwitchOff = _config.autoBan;
            if (_config.authUser != null && _config.authUser.Length > 0)
            {
                handler.authConnection = AuthConnection;
                handler.authUser = _config.authUser ?? "";
                handler.authPass = _config.authPass ?? "";
            }
            if (!EncryptorLoaded)
            {
                try
                {
                    handler.server = _config.GetCurrentServer(null, true);
                    IEncryptor encryptor = EncryptorFactory.GetEncryptor(handler.server.method, handler.server.password);
                    encryptor.Dispose();
                }
                catch
                {

                }
                finally
                {
                    EncryptorLoaded = true;
                    handler.server = null;
                }
            }

            if (_config.GetPortMapCache().ContainsKey(local_port))
            {
                PortMapConfigCache cfg = _config.GetPortMapCache()[local_port];
                if (cfg.id == cfg.server.id)
                {
                    handler.select_server = cfg.server;
                    handler.command = 1;
                    byte[] addr = System.Text.Encoding.UTF8.GetBytes(cfg.server_addr);
                    byte[] newFirstPacket = new byte[length + addr.Length + 4];
                    newFirstPacket[0] = 3;
                    newFirstPacket[1] = (byte)addr.Length;
                    Array.Copy(addr, 0, newFirstPacket, 2, addr.Length);
                    newFirstPacket[addr.Length + 2] = (byte)(cfg.server_port / 256);
                    newFirstPacket[addr.Length + 3] = (byte)(cfg.server_port % 256);
                    Array.Copy(firstPacket, 0, newFirstPacket, addr.Length + 4, length);
                    handler.Start(newFirstPacket, newFirstPacket.Length, true);
                    return true;
                }
            }

            handler.Start(firstPacket, length, false);
            return true;
        }
    }

    class ProtocolResponseDetector
    {
        public enum Protocol
        {
            UNKONWN = -1,
            NOTBEGIN = 0,
            HTTP = 1,
            TLS = 2,
            SOCKS4 = 4,
            SOCKS5 = 5,
        }
        protected Protocol protocol = Protocol.NOTBEGIN;
        protected byte[] send_buffer = new byte[0];
        protected byte[] recv_buffer = new byte[0];

        public bool Pass
        {
            get; set;
        }

        public ProtocolResponseDetector()
        {
            Pass = false;
        }

        public void OnSend(byte[] send_data, int length)
        {
            if (protocol != Protocol.NOTBEGIN) return;
            Array.Resize(ref send_buffer, send_buffer.Length + length);
            Array.Copy(send_data, 0, send_buffer, send_buffer.Length - length, length);

            if (send_buffer.Length < 2) return;

            int head_size = Obfs.ObfsBase.GetHeadSize(send_buffer, send_buffer.Length);
            if (send_buffer.Length - head_size < 0) return;
            byte[] data = new byte[send_buffer.Length - head_size];
            Array.Copy(send_buffer, head_size, data, 0, data.Length);

            if (data.Length < 2) return;

            if (data.Length > 8)
            {
                if (data[0] == 22 && data[1] == 3 && (data[2] >= 0 && data[2] <= 3))
                {
                    protocol = Protocol.TLS;
                    return;
                }
                if (data[0] == 'G' && data[1] == 'E' && data[2] == 'T' && data[3] == ' '
                    || data[0] == 'P' && data[1] == 'U' && data[2] == 'T' && data[3] == ' '
                    || data[0] == 'H' && data[1] == 'E' && data[2] == 'A' && data[3] == 'D' && data[4] == ' '
                    || data[0] == 'P' && data[1] == 'O' && data[2] == 'S' && data[3] == 'T' && data[4] == ' '
                    || data[0] == 'C' && data[1] == 'O' && data[2] == 'N' && data[3] == 'N' && data[4] == 'E' && data[5] == 'C' && data[6] == 'T' && data[7] == ' '
                    )
                {
                    protocol = Protocol.HTTP;
                    return;
                }
            }
            else
            {
                protocol = Protocol.UNKONWN;
            }
        }
        public int OnRecv(byte[] recv_data, int length)
        {
            if (protocol == Protocol.UNKONWN || protocol == Protocol.NOTBEGIN) return 0;
            Array.Resize(ref recv_buffer, recv_buffer.Length + length);
            Array.Copy(recv_data, 0, recv_buffer, recv_buffer.Length - length, length);

            if (recv_buffer.Length < 2) return 0;

            if (protocol == Protocol.HTTP && recv_buffer.Length > 4)
            {
                if (recv_buffer[0] == 'H' && recv_buffer[1] == 'T' && recv_buffer[2] == 'T' && recv_buffer[3] == 'P')
                {
                    Finish();
                    return 0;
                }
                else
                {
                    protocol = Protocol.UNKONWN;
                    return 1;
                    //throw new ProtocolException("Wrong http response");
                }
            }
            else if (protocol == Protocol.TLS && recv_buffer.Length > 4)
            {
                if (recv_buffer[0] == 22 && recv_buffer[1] == 3)
                {
                    Finish();
                    return 0;
                }
                else
                {
                    protocol = Protocol.UNKONWN;
                    return 2;
                    //throw new ProtocolException("Wrong tls response");
                }
            }
            return 0;
        }

        protected void Finish()
        {
            send_buffer = null;
            recv_buffer = null;
            protocol = Protocol.UNKONWN;
            Pass = true;
        }
    }

    class Handler
    {
        public delegate Server GetCurrentServer(string targetURI = null, bool usingRandom = false, bool forceRandom = false);
        public delegate void KeepCurrentServer(string targetURI);
        public delegate bool AuthConnection(Socket connection, string authUser, string authPass);
        public GetCurrentServer getCurrentServer;
        public KeepCurrentServer keepCurrentServer;
        public Server server;
        public Server select_server;
        public Double TTL = 0; // Second
        public int try_keep_alive = 0;
        // Connection socket
        public Socket connection;
        public Socket connectionUDP;
        protected IPEndPoint connectionUDPEndPoint;
        // Server proxy
        public int proxyType = 0;
        public string socks5RemoteHost;
        public int socks5RemotePort = 0;
        public string socks5RemoteUsername;
        public string socks5RemotePassword;
        public AuthConnection authConnection;
        // auto ban
        public bool autoSwitchOff = true;
        // Reconnect
        public int reconnectTimesRemain = 0;
        protected int reconnectTimes = 0;
        public bool forceRandom = false;
        // Encryptor
        protected IEncryptor encryptor;
        protected IEncryptor encryptorUDP;
        //
        public IObfs protocol;
        public IObfs obfs;
        protected ProtocolResponseDetector detector = new ProtocolResponseDetector();
        // remote socket.
        protected Socket remote;
        protected Socket remoteUDP;
        protected IPEndPoint remoteTCPEndPoint;
        protected IPEndPoint remoteUDPEndPoint;
        // Connect command
        public byte command;
        // Init data
        protected byte[] _firstPacket;
        protected int _firstPacketLength;
        // Size of receive buffer.
        protected const int RecvSize = 16384;
        protected const int BufferSize = RecvSize + 1024;
        protected const int AutoSwitchOffErrorTimes = 5;
        // remote receive buffer
        protected byte[] remoteRecvBuffer = new byte[RecvSize * 2];
        // remote send buffer
        protected byte[] remoteSendBuffer = new byte[BufferSize * 2];
        // remote header send buffer
        protected byte[] remoteHeaderSendBuffer;
        // http proxy
        public string authUser;
        public string authPass;
        protected HttpPraser httpProxyState;
        // connection receive buffer
        protected byte[] connetionRecvBuffer = new byte[RecvSize * 2];
        // connection send buffer
        protected byte[] connetionSendBuffer = new byte[BufferSize * 2];
        // connection send buffer
        protected List<byte[]> connectionSendBufferList = new List<byte[]>();
        protected string targetURI;
        protected DateTime lastKeepTime;

        protected byte[] remoteUDPRecvBuffer = new byte[RecvSize * 2];
        protected int remoteUDPRecvBufferLength = 0;

        protected bool closed = false;

        protected object encryptionLock = new object();
        protected object decryptionLock = new object();
        protected object obfsLock = new object();
        protected object recvUDPoverTCPLock = new object();

        protected bool connectionTCPIdle;
        protected bool connectionUDPIdle;
        protected bool remoteTCPIdle;
        protected bool remoteUDPIdle;
        protected int remoteRecvCount = 0;
        protected int connectionRecvCount = 0;

        protected IAsyncResult arTCPConnection;
        protected IAsyncResult arTCPRemote;
        protected IAsyncResult arUDPConnection;
        protected IAsyncResult arUDPRemote;

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

            try_keep_alive = 0;
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
                        timer.Start();
                    }
                    else
                    {
                        timer.Interval = time * 1000.0;
                        timer.Stop();
                        timer.Start();
                    }
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
            Dictionary<string, int> protocols = new Dictionary<string, int>();
            protocols["auth_sha1_v2"] = 1;

            try
            {
                if (try_keep_alive <= 0 && State == ConnectState.CONNECTED && protocols.ContainsKey(this.protocol.Name()))
                {
                    try_keep_alive++;
                    RemoteSend(null, -1);
                }
                else
                {
                    if (connection != null)
                    {
                        if (remote != null && reconnectTimesRemain > 0
                            //&& obfs != null && obfs.getSentLength() == 0
                            && connectionSendBufferList != null
                            && (State == ConnectState.CONNECTED || State == ConnectState.CONNECTING))
                        {
                            remote.Shutdown(SocketShutdown.Both);
                        }
                        else
                        {
                            Server s = server;
                            if (s != null && connectionSendBufferList != null)
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
                            && autoSwitchOff)
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
                            && autoSwitchOff)
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
                                && autoSwitchOff)
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
                            if (s.ServerSpeedLog().ErrorConnectTimes >= 3 && autoSwitchOff)
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
                                && autoSwitchOff)
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
                                && autoSwitchOff)
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
            if (1 > 0)
            {
                Handler handler = new Handler();
                handler.getCurrentServer = getCurrentServer;
                handler.keepCurrentServer = keepCurrentServer;
                handler.authConnection = authConnection;
                handler.select_server = select_server;
                handler.connection = connection;
                handler.connectionUDP = connectionUDP;
                handler.reconnectTimesRemain = reconnectTimesRemain - 1;
                handler.reconnectTimes = reconnectTimes + 1;
                handler.forceRandom = forceRandom;
                handler.targetURI = targetURI;
                handler.command = command;

                handler.speedTester.transfer = speedTester.transfer;
                handler.proxyType = proxyType;
                handler.socks5RemoteHost = socks5RemoteHost;
                handler.socks5RemotePort = socks5RemotePort;
                handler.socks5RemoteUsername = socks5RemoteUsername;
                handler.socks5RemotePassword = socks5RemotePassword;
                handler.TTL = TTL;
                handler.autoSwitchOff = autoSwitchOff;
                handler.authConnection = authConnection;
                handler.authUser = authUser;
                handler.authPass = authPass;

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
                else
                {
                    //return false;
                }
                handler.Start(newFirstPacket, newFirstPacket.Length, true);
                return true;
            }
            else
            {
                if (obfs != null)
                {
                    obfs.Dispose();
                    obfs = null;
                }
                if (protocol != null)
                {
                    protocol.Dispose();
                    protocol = null;
                }

                ResetTimeout(TTL);

                reconnectTimesRemain--;
                reconnectTimes++;

                Server s = server;
                if (this.State != ConnectState.HANDSHAKE && this.State != ConnectState.READY && s != null)
                {
                    lock (this)
                    {
                        s.ServerSpeedLog().AddDisconnectTimes();
                        s.GetConnections().DecRef(this.connection);
                    }
                }
                this.State = ConnectState.HANDSHAKE;
                CloseSocket(ref remote);
                CloseSocket(ref remoteUDP);

                speedTester.sizeUpload = 0;
                speedTester.sizeDownload = 0;
                speedTester.sizeRecv = 0;

                lastErrCode = 0;
                for (int i = 0; i < 3
                    && (
                    (arTCPConnection == null || !arTCPConnection.IsCompleted)
                    || (arTCPRemote == null || !arTCPRemote.IsCompleted)); ++i)
                {
                    Thread.Sleep(100);
                }

                try
                {
                    Connect();
                }
                catch (Exception e)
                {
                    LogSocketException(e);
                    string remarks;
                    string server_url = getServerUrl(out remarks);
                    if (!Logging.LogSocketException(remarks, server_url, e))
                        Logging.LogUsefulException(e);
                    this.Close();
                }
            }
            return true;
        }

        public void Start(byte[] firstPacket, int length, bool tunnel)
        {
            this._firstPacket = firstPacket;
            this._firstPacketLength = length;
            if (socks5RemotePort > 0)
            {
                autoSwitchOff = false;
            }
            ResetTimeout(TTL);
            if (this.State == ConnectState.READY)
            {
                this.State = ConnectState.HANDSHAKE;
                if (tunnel)
                {
                    remoteHeaderSendBuffer = firstPacket;
                    this.Connect();
                }
                else
                {
                    this.HandshakeReceive();
                }
            }
        }

        private void BeginConnect(IPAddress ipAddress, int serverPort)
        {
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);
            remoteTCPEndPoint = remoteEP;
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

            if (socks5RemotePort != 0
                || connectionUDP == null
                || connectionUDP != null && server.udp_over_tcp)
            {
                remote = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }

            if (connectionUDP != null && !server.udp_over_tcp)
            {
                try
                {
                    remoteUDP = new Socket(ipAddress.AddressFamily,
                        SocketType.Dgram, ProtocolType.Udp);
                    remoteUDP.Bind(new IPEndPoint(ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0));
                }
                catch (SocketException)
                {
                    remoteUDP = null;
                }
            }
            ResetTimeout(TTL);

            // Connect to the remote endpoint.
            if (socks5RemotePort == 0 && connectionUDP != null && !server.udp_over_tcp)
            {
                ConnectState _state = this.State;
                if (_state == ConnectState.CONNECTING)
                {
                    StartPipe();
                }
                else if (_state == ConnectState.CONNECTED)
                {
                    //ERROR
                }
            }
            else
            {
                speedTester.BeginConnect();
                IAsyncResult result = remote.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), new CallbackStatus());
                double t = TTL;
                if (t <= 0) t = 40;
                else if (t <= 10) t = 10;
                if (reconnectTimesRemain + reconnectTimes > 0 || TTL > 0)
                {
                    bool success = result.AsyncWaitHandle.WaitOne((int)(t * 250), true);
                    if (!success)
                    {
                        ((CallbackStatus)result.AsyncState).SetIfEqu(-1, 0);
                        if (((CallbackStatus)result.AsyncState).Status == -1)
                        {
                            lastErrCode = 8;
                            server.ServerSpeedLog().AddTimeoutTimes();
                            CloseSocket(ref remote);
                            Close();
                        }
                    }
                }
            }
        }

        private void DnsCallback(IAsyncResult ar)
        {
            ((CallbackStatus)ar.AsyncState).SetIfEqu(1, 0);
            if (closed || ((CallbackStatus)ar.AsyncState).Status != 1)
            {
                return;
            }
            try
            {
                IPAddress ipAddress;
                IPHostEntry ipHostInfo = Dns.EndGetHostEntry(ar);
                ipAddress = ipHostInfo.AddressList[0];
                int serverPort = server.server_port;
                if (socks5RemotePort > 0)
                {
                    server.DnsBuffer().UpdateDns(socks5RemoteHost, ipAddress);
                    serverPort = socks5RemotePort;
                }
                else
                {
                    server.DnsBuffer().UpdateDns(server.server, ipAddress);
                }
                BeginConnect(ipAddress, serverPort);
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        public bool TryReconnect()
        {
            if (this.State == ConnectState.CONNECTING)
            {
                if (reconnectTimesRemain > 0)
                {
                    return this.ReConnect();
                }
            }
            else if (this.State == ConnectState.CONNECTED)
            {
                if (obfs.getSentLength() == 0 && connectionSendBufferList != null)
                {
                    if (reconnectTimesRemain > 0)
                    {
                        this.State = ConnectState.CONNECTING;
                        return this.ReConnect();
                    }
                }
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
                    //sock = null;
                    try
                    {
                        s.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }
                    try
                    {
                        s.Close();
                    }
                    catch
                    {
                    }
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
            //try
            //{
            //    if (arTCPConnection != null && !arTCPConnection.IsCompleted)
            //    {
            //        int bytesRead = endConnectionTCPRecv(arTCPConnection);
            //        if (bytesRead > 0)
            //        {
            //            if (connectionSendBufferList != null)
            //            {
            //                detector.OnSend(connetionRecvBuffer, bytesRead);
            //                byte[] data = new byte[bytesRead];
            //                Array.Copy(connetionRecvBuffer, data, data.Length);
            //                connectionSendBufferList.Add(data);
            //            }
            //        }
            //    }
            //}
            //catch
            //{

            //}
            //try
            //{
            //    if (arTCPRemote != null && !arTCPRemote.IsCompleted)
            //    {
            //        int bytesRead = endRemoteTCPRecv(arTCPRemote);
            //    }
            //}
            //catch
            //{

            //}
            //try
            //{
            //    if (arUDPConnection != null && !arUDPConnection.IsCompleted)
            //    {
            //        IPEndPoint sender = new IPEndPoint(connectionUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            //        EndPoint tempEP = (EndPoint)sender;

            //        int bytesRead = endConnectionUDPRecv(arUDPConnection, ref tempEP);
            //    }
            //}
            //catch
            //{

            //}
            //try
            //{
            //    if (arUDPRemote != null && !arUDPRemote.IsCompleted)
            //    {
            //        IPEndPoint sender = new IPEndPoint(remoteUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            //        EndPoint tempEP = (EndPoint)sender;

            //        int bytesRead = endRemoteUDPRecv(arUDPRemote, ref tempEP);
            //    }
            //}
            //catch
            //{

            //}
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
            for (int i = 0; i < 10; ++i)
            {
                if ((arTCPConnection == null || !arTCPConnection.IsCompleted)
                    && (arTCPRemote == null || !arTCPRemote.IsCompleted)
                    && (arUDPConnection == null || !arUDPConnection.IsCompleted)
                    && (arUDPRemote == null || !arUDPRemote.IsCompleted)
                    )
                    break;
                Thread.Sleep(10 * (i + 1) * (i + 1));
            }
            if (lastErrCode == 0 && server != null)
            {
                if (speedTester.sizeRecv == 0 && speedTester.sizeUpload > 0)
                    server.ServerSpeedLog().AddErrorEmptyTimes();
                else
                    server.ServerSpeedLog().AddNoErrorTimes();
            }
            keepCurrentServer(targetURI);
            //int type = speedTester.GetActionType();
            //if (type > 0)
            //{
            //    Console.WriteLine("Connection to " + targetURI + " type " + type.ToString());
            //}
            ResetTimeout(0);
            try
            {
                bool reconnect = TryReconnect();
                //if (reconnect) return;
                //lock (this)
                {
                    if (this.State != ConnectState.END)
                    {
                        if (this.State != ConnectState.READY && this.State != ConnectState.HANDSHAKE && server != null)
                        {
                            server.ServerSpeedLog().AddDisconnectTimes();
                            server.GetConnections().DecRef(this.connection);
                            server.ServerSpeedLog().AddSpeedLog(new TransLog((int)speedTester.GetAvgDownloadSpeed(), DateTime.Now));
                        }
                        this.State = ConnectState.END;
                    }
                    getCurrentServer = null;
                    keepCurrentServer = null;
                    authConnection = null;
                }

                if (!reconnect)
                {
                    CloseSocket(ref connection);
                    CloseSocket(ref connectionUDP);
                }
                else
                {
                    connection = null;
                    connectionUDP = null;
                }
                if (obfs != null)
                {
                    obfs.Dispose();
                    obfs = null;
                }
                if (protocol != null)
                {
                    protocol.Dispose();
                    protocol = null;
                }
                lock (encryptionLock)
                {
                    lock (decryptionLock)
                    {
                        if (encryptor != null)
                            ((IDisposable)encryptor).Dispose();
                        if (encryptorUDP != null)
                            ((IDisposable)encryptorUDP).Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private bool ConnectHttpProxyServer(string strRemoteHost, int iRemotePort, Socket sProxyServer, int socketErrorCode)
        {
            IPAddress ipAdd;
            bool ForceRemoteDnsResolve = false;
            bool parsed = IPAddress.TryParse(strRemoteHost, out ipAdd);
            if (!parsed && !ForceRemoteDnsResolve)
            {
                if (server.DnsTargetBuffer().isExpired(strRemoteHost))
                {
                    try
                    {
                        IPHostEntry ipHostInfo = Dns.GetHostEntry(strRemoteHost);
                        ipAdd = ipHostInfo.AddressList[0];
                        server.DnsTargetBuffer().UpdateDns(strRemoteHost, ipAdd);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    ipAdd = server.DnsTargetBuffer().ip;
                }
            }
            if (ipAdd != null)
            {
                strRemoteHost = ipAdd.ToString();
            }
            string host = (strRemoteHost.IndexOf(':') >= 0 ? "[" + strRemoteHost + "]" : strRemoteHost) + ":" + iRemotePort.ToString();
            string authstr = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(socks5RemoteUsername + ":" + socks5RemotePassword));
            string cmd = "CONNECT " + host + " HTTP/1.0\r\n"
                + "Host: " + host + "\r\n"
                + "Proxy-Connection: Keep-Alive\r\n";
            if (socks5RemoteUsername.Length > 0)
                cmd += "Proxy-Authorization: Basic " + authstr + "\r\n";
            cmd += "\r\n";
            byte[] httpData = System.Text.Encoding.UTF8.GetBytes(cmd);
            sProxyServer.Send(httpData, httpData.Length, SocketFlags.None);
            byte[] byReceive = new byte[1024];
            int iRecCount = sProxyServer.Receive(byReceive, byReceive.Length, SocketFlags.None);
            if (iRecCount > 13)
            {
                string data = System.Text.Encoding.UTF8.GetString(byReceive, 0, iRecCount);
                if (data.StartsWith("HTTP/1.1 200") || data.StartsWith("HTTP/1.0 200"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool ConnectSocks5ProxyServer(string strRemoteHost, int iRemotePort, Socket sProxyServer, int socketErrorCode)
        {
            //构造Socks5代理服务器第一连接头(无用户名密码)
            byte[] bySock5Send = new Byte[10];
            bySock5Send[0] = 5;
            bySock5Send[1] = 2;
            bySock5Send[2] = 0;
            bySock5Send[3] = 2;

            //发送Socks5代理第一次连接信息
            sProxyServer.Send(bySock5Send, 4, SocketFlags.None);

            byte[] bySock5Receive = new byte[32];
            int iRecCount = sProxyServer.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

            if (iRecCount < 2)
            {
                throw new SocketException(socketErrorCode);
                //throw new Exception("不能获得代理服务器正确响应。");
            }

            if (bySock5Receive[0] != 5 || (bySock5Receive[1] != 0 && bySock5Receive[1] != 2))
            {
                throw new SocketException(socketErrorCode);
                //throw new Exception("代理服务其返回的响应错误。");
            }

            if (bySock5Receive[1] != 0) // auth
            {
                if (bySock5Receive[1] == 2)
                {
                    if (socks5RemoteUsername.Length == 0)
                    {
                        throw new SocketException(socketErrorCode);
                        //throw new Exception("代理服务器需要进行身份确认。");
                    }
                    else
                    {
                        bySock5Send = new Byte[socks5RemoteUsername.Length + socks5RemotePassword.Length + 3];
                        bySock5Send[0] = 1;
                        bySock5Send[1] = (Byte)socks5RemoteUsername.Length;
                        for (int i = 0; i < socks5RemoteUsername.Length; ++i)
                        {
                            bySock5Send[2 + i] = (Byte)socks5RemoteUsername[i];
                        }
                        bySock5Send[socks5RemoteUsername.Length + 2] = (Byte)socks5RemotePassword.Length;
                        for (int i = 0; i < socks5RemotePassword.Length; ++i)
                        {
                            bySock5Send[socks5RemoteUsername.Length + 3 + i] = (Byte)socks5RemotePassword[i];
                        }
                        sProxyServer.Send(bySock5Send, bySock5Send.Length, SocketFlags.None);
                        iRecCount = sProxyServer.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

                        if (bySock5Receive[0] != 1 || bySock5Receive[1] != 0)
                        {
                            throw new SocketException((int)SocketError.ConnectionRefused);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            // connect
            if (command == 1) // TCP
            {
                List<byte> dataSock5Send = new List<byte>();
                dataSock5Send.Add(5);
                dataSock5Send.Add(1);
                dataSock5Send.Add(0);

                IPAddress ipAdd;
                bool ForceRemoteDnsResolve = false;
                bool parsed = IPAddress.TryParse(strRemoteHost, out ipAdd);
                if (!parsed && !ForceRemoteDnsResolve)
                {
                    if (server.DnsTargetBuffer().isExpired(strRemoteHost))
                    {
                        try
                        {
                            IPHostEntry ipHostInfo = Dns.GetHostEntry(strRemoteHost);
                            ipAdd = ipHostInfo.AddressList[0];
                            server.DnsTargetBuffer().UpdateDns(strRemoteHost, ipAdd);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        ipAdd = server.DnsTargetBuffer().ip;
                    }
                }
                if (ipAdd == null)
                {
                    dataSock5Send.Add(3); // remote DNS resolve
                    dataSock5Send.Add((byte)strRemoteHost.Length);
                    for (int i = 0; i < strRemoteHost.Length; ++i)
                    {
                        dataSock5Send.Add((byte)strRemoteHost[i]);
                    }
                }
                else
                {
                    byte[] addBytes = ipAdd.GetAddressBytes();
                    if (addBytes.GetLength(0) > 4)
                    {
                        dataSock5Send.Add(4); // IPv6
                        for (int i = 0; i < 16; ++i)
                        {
                            dataSock5Send.Add(addBytes[i]);
                        }
                    }
                    else
                    {
                        dataSock5Send.Add(1); // IPv4
                        for (int i = 0; i < 4; ++i)
                        {
                            dataSock5Send.Add(addBytes[i]);
                        }
                    }
                }

                dataSock5Send.Add((byte)(iRemotePort / 256));
                dataSock5Send.Add((byte)(iRemotePort % 256));

                sProxyServer.Send(dataSock5Send.ToArray(), dataSock5Send.Count, SocketFlags.None);
                iRecCount = sProxyServer.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

                if (iRecCount < 2 || bySock5Receive[0] != 5 || bySock5Receive[1] != 0)
                {
                    throw new SocketException(socketErrorCode);
                    //throw new Exception("第二次连接Socks5代理返回数据出错。");
                }
                return true;
            }
            else if (command == 3) // UDP
            {
                List<byte> dataSock5Send = new List<byte>();
                dataSock5Send.Add(5);
                dataSock5Send.Add(3);
                dataSock5Send.Add(0);

                IPAddress ipAdd = remoteUDPEndPoint.Address;
                {
                    byte[] addBytes = ipAdd.GetAddressBytes();
                    if (addBytes.GetLength(0) > 4)
                    {
                        dataSock5Send.Add(4); // IPv6
                        for (int i = 0; i < 16; ++i)
                        {
                            dataSock5Send.Add(addBytes[i]);
                        }
                    }
                    else
                    {
                        dataSock5Send.Add(1); // IPv4
                        for (int i = 0; i < 4; ++i)
                        {
                            dataSock5Send.Add(addBytes[i]);
                        }
                    }
                }

                dataSock5Send.Add((byte)(0));
                dataSock5Send.Add((byte)(0));

                sProxyServer.Send(dataSock5Send.ToArray(), dataSock5Send.Count, SocketFlags.None);
                iRecCount = sProxyServer.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

                if (bySock5Receive[0] != 5 || bySock5Receive[1] != 0)
                {
                    throw new SocketException(socketErrorCode);
                    //throw new Exception("第二次连接Socks5代理返回数据出错。");
                }
                else
                {
                    bool ipv6 = bySock5Receive[0] == 4;
                    byte[] addr;
                    int port;
                    if (!ipv6)
                    {
                        addr = new byte[4];
                        Array.Copy(bySock5Receive, 4, addr, 0, 4);
                        port = bySock5Receive[8] * 0x100 + bySock5Receive[9];
                    }
                    else
                    {
                        addr = new byte[16];
                        Array.Copy(bySock5Receive, 4, addr, 0, 16);
                        port = bySock5Receive[20] * 0x100 + bySock5Receive[21];
                    }
                    ipAdd = new IPAddress(addr);
                    remoteUDPEndPoint = new IPEndPoint(ipAdd, port);
                }
                return true;
            }
            return false;
        }

        private bool ConnectProxyServer(string strRemoteHost, int iRemotePort, Socket sProxyServer, int socketErrorCode)
        {
            if (proxyType == 0)
            {
                return ConnectSocks5ProxyServer(strRemoteHost, iRemotePort, sProxyServer, socketErrorCode);
            }
            else if (proxyType == 1)
            {
                return ConnectHttpProxyServer(strRemoteHost, iRemotePort, sProxyServer, socketErrorCode);
            }
            else
            {
                return true;
            }
        }

        private void RspHttpHandshakeReceive()
        {
            command = 1; // Set TCP connect command
            if (httpProxyState == null)
            {
                httpProxyState = new HttpPraser();
            }
            else
            {
                command = 1;
            }
            if (Util.Utils.isMatchSubNet(((IPEndPoint)connection.RemoteEndPoint).Address, "127.0.0.0/8"))
            {
                httpProxyState.httpAuthUser = "";
                httpProxyState.httpAuthPass = "";
            }
            else
            {
                httpProxyState.httpAuthUser = authUser;
                httpProxyState.httpAuthPass = authPass;
            }
            int err = httpProxyState.HandshakeReceive(_firstPacket, _firstPacketLength, ref remoteHeaderSendBuffer);
            if (err == 1)
            {
                connection.BeginReceive(connetionRecvBuffer, 0, _firstPacket.Length, 0,
                    new AsyncCallback(HttpHandshakeRecv), null);
            }
            else if (err == 2)
            {
                string dataSend = httpProxyState.Http407();
                byte[] httpData = System.Text.Encoding.UTF8.GetBytes(dataSend);
                connection.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(HttpHandshakeAuthEndSend), null);
            }
            else if (err == 3)
            {
                Connect();
            }
            else if (err == 4)
            {
                Connect();
            }
            else if (err == 0)
            {
                string dataSend = httpProxyState.Http200();
                byte[] httpData = System.Text.Encoding.UTF8.GetBytes(dataSend);
                connection.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(StartConnect), null);
            }
            else if (err == 500)
            {
                string dataSend = httpProxyState.Http500();
                byte[] httpData = System.Text.Encoding.UTF8.GetBytes(dataSend);
                connection.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(HttpHandshakeAuthEndSend), null);
            }
        }

        private void HttpHandshakeAuthEndSend(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connection.EndSend(ar);
                connection.BeginReceive(connetionRecvBuffer, 0, _firstPacket.Length, 0,
                    new AsyncCallback(HttpHandshakeRecv), null);
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void HttpHandshakeRecv(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                int bytesRead = connection.EndReceive(ar);
                if (bytesRead > 0)
                {
                    Array.Copy(connetionRecvBuffer, _firstPacket, bytesRead);
                    _firstPacketLength = bytesRead;
                    RspHttpHandshakeReceive();
                }
                else
                {
                    Console.WriteLine("failed to recv data in HttpHandshakeRecv");
                    this.Close();
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void RspSocks4aHandshakeReceive()
        {
            List<byte> firstPacket = new List<byte>();
            for (int i = 0; i < _firstPacketLength; ++i)
            {
                firstPacket.Add(_firstPacket[i]);
            }
            List<byte> dataSockSend = firstPacket.GetRange(0, 4);
            dataSockSend[0] = 0;
            dataSockSend[1] = 90;

            bool remoteDNS = (_firstPacket[4] == 0 && _firstPacket[5] == 0 && _firstPacket[6] == 0 && _firstPacket[7] == 1) ? true : false;
            if (remoteDNS)
            {
                for (int i = 0; i < 4; ++i)
                {
                    dataSockSend.Add(0);
                }
                int addrStartPos = firstPacket.IndexOf(0x0, 8);
                List<byte> addr = firstPacket.GetRange(addrStartPos + 1, firstPacket.Count - addrStartPos - 2);
                remoteHeaderSendBuffer = new byte[2 + addr.Count + 2];
                remoteHeaderSendBuffer[0] = 3;
                remoteHeaderSendBuffer[1] = (byte)addr.Count;
                Array.Copy(addr.ToArray(), 0, remoteHeaderSendBuffer, 2, addr.Count);
                remoteHeaderSendBuffer[2 + addr.Count] = dataSockSend[2];
                remoteHeaderSendBuffer[2 + addr.Count + 1] = dataSockSend[3];
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    dataSockSend.Add(_firstPacket[4 + i]);
                }
                remoteHeaderSendBuffer = new byte[1 + 4 + 2];
                remoteHeaderSendBuffer[0] = 1;
                Array.Copy(dataSockSend.ToArray(), 4, remoteHeaderSendBuffer, 1, 4);
                remoteHeaderSendBuffer[1 + 4] = dataSockSend[2];
                remoteHeaderSendBuffer[1 + 4 + 1] = dataSockSend[3];
            }
            command = 1; // Set TCP connect command
            connection.BeginSend(dataSockSend.ToArray(), 0, dataSockSend.Count, 0, new AsyncCallback(StartConnect), null);
        }

        private void RspSocks5HandshakeReceive()
        {
            byte[] response = { 5, 0 };
            if (_firstPacket[0] != 5)
            {
                response = new byte[] { 0, 91 };
                Console.WriteLine("socks 4/5 protocol error");
            }
            if (authConnection != null && !Util.Utils.isMatchSubNet(((IPEndPoint)connection.RemoteEndPoint).Address, "127.0.0.0/8") )
            {
                response[1] = 2;
                connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(HandshakeAuthSendCallback), null);
            }
            else
            {
                connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(HandshakeSendCallback), null);
            }
        }

        private void HandshakeReceive()
        {
            if (closed)
            {
                return;
            }
            try
            {
                int bytesRead = _firstPacketLength;

                if (bytesRead > 1)
                {
                    if ((authConnection == null || Util.Utils.isMatchSubNet(((IPEndPoint)connection.RemoteEndPoint).Address, "127.0.0.0/8"))
                        && _firstPacket[0] == 4 && _firstPacketLength >= 9)
                    {
                        RspSocks4aHandshakeReceive();
                    }
                    else if (_firstPacket[0] == 5 && _firstPacketLength >= 2)
                    {
                        RspSocks5HandshakeReceive();
                    }
                    else
                    {
                        RspHttpHandshakeReceive();
                    }
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void HandshakeAuthSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connection.EndSend(ar);

                connection.BeginReceive(connetionRecvBuffer, 0, 1024, 0,
                    new AsyncCallback(HandshakeAuthReceiveCallback), null);
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void HandshakeAuthReceiveCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead >= 3)
                {
                    byte user_len = connetionRecvBuffer[1];
                    byte pass_len = connetionRecvBuffer[user_len + 2];
                    byte[] response = { 1, 0 };
                    string user = Encoding.UTF8.GetString(connetionRecvBuffer, 2, user_len);
                    string pass = Encoding.UTF8.GetString(connetionRecvBuffer, user_len + 3, pass_len);
                    if (authConnection(connection, user, pass))
                    {
                        connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(HandshakeSendCallback), null);
                    }
                }
                else
                {
                    Console.WriteLine("failed to recv data in HandshakeAuthReceiveCallback");
                    this.Close();
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void HandshakeSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connection.EndSend(ar);

                // +----+-----+-------+------+----------+----------+
                // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                connection.BeginReceive(connetionRecvBuffer, 0, 1024, 0,
                    new AsyncCallback(HandshakeReceive2Callback), null);
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void RspSocks5UDPHeader(int bytesRead)
        {
            bool ipv6 = connection.AddressFamily == AddressFamily.InterNetworkV6;
            int udpPort = 0;
            if (bytesRead >= 3 + 6)
            {
                ipv6 = remoteHeaderSendBuffer[0] == 4;
                if (!ipv6)
                    udpPort = remoteHeaderSendBuffer[5] * 0x100 + remoteHeaderSendBuffer[6];
                else
                    udpPort = remoteHeaderSendBuffer[17] * 0x100 + remoteHeaderSendBuffer[18];
            }
            if (!ipv6)
            {
                remoteHeaderSendBuffer = new byte[1 + 4 + 2];
                remoteHeaderSendBuffer[0] = 0x8 | 1;
                remoteHeaderSendBuffer[5] = (byte)(udpPort / 0x100);
                remoteHeaderSendBuffer[6] = (byte)(udpPort % 0x100);
            }
            else
            {
                remoteHeaderSendBuffer = new byte[1 + 16 + 2];
                remoteHeaderSendBuffer[0] = 0x8 | 4;
                remoteHeaderSendBuffer[17] = (byte)(udpPort / 0x100);
                remoteHeaderSendBuffer[18] = (byte)(udpPort % 0x100);
            }

            connectionUDPEndPoint = null;
            int port = 0;
            IPAddress ip = ipv6 ? IPAddress.IPv6Any : IPAddress.Any;
            connectionUDP = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            for (; port < 65536; ++port)
            {
                try
                {
                    connectionUDP.Bind(new IPEndPoint(ip, port));
                    break;
                }
                catch (Exception)
                {
                    //
                }
            }
            port = ((IPEndPoint)connectionUDP.LocalEndPoint).Port;
            if (!ipv6)
            {
                byte[] response = { 5, 0, 0, 1,
                                0, 0, 0, 0,
                                (byte)(port / 0x100), (byte)(port % 0x100) };
                byte[] ip_bytes = ((IPEndPoint)connection.LocalEndPoint).Address.GetAddressBytes();
                Array.Copy(ip_bytes, 0, response, 4, 4);
                connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
            }
            else
            {
                byte[] response = { 5, 0, 0, 4,
                                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                (byte)(port / 0x100), (byte)(port % 0x100) };
                byte[] ip_bytes = ((IPEndPoint)connection.LocalEndPoint).Address.GetAddressBytes();
                Array.Copy(ip_bytes, 0, response, 4, 16);
                connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
            }
        }

        private void RspSocks5TCPHeader()
        {
            if (connection.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] response = { 5, 0, 0, 1,
                                0, 0, 0, 0,
                                0, 0 };
                connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
            }
            else
            {
                byte[] response = { 5, 0, 0, 4,
                                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                0, 0 };
                connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
            }
        }

        private void HandshakeReceive2Callback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead >= 3)
                {
                    command = connetionRecvBuffer[1];
                    if (bytesRead > 3)
                    {
                        remoteHeaderSendBuffer = new byte[bytesRead - 3];
                        Array.Copy(connetionRecvBuffer, 3, remoteHeaderSendBuffer, 0, remoteHeaderSendBuffer.Length);
                    }
                    else
                    {
                        remoteHeaderSendBuffer = null;
                    }

                    if (command == 3) // UDP
                    {
                        if (bytesRead >= 5)
                        {
                            if (remoteHeaderSendBuffer == null)
                            {
                                remoteHeaderSendBuffer = new byte[bytesRead - 3];
                                Array.Copy(connetionRecvBuffer, 3, remoteHeaderSendBuffer, 0, remoteHeaderSendBuffer.Length);
                            }
                            else
                            {
                                Array.Resize(ref remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length + bytesRead - 3);
                                Array.Copy(connetionRecvBuffer, 3, remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length - bytesRead, bytesRead);
                            }

                            RspSocks5UDPHeader(bytesRead);
                        }
                        else
                        {
                            Console.WriteLine("failed to recv data in HandshakeReceive2Callback");
                            this.Close();
                        }
                    }
                    else
                    {
                        RspSocks5TCPHeader();
                    }
                }
                else
                {
                    Console.WriteLine("failed to recv data in HandshakeReceive2Callback");
                    System.Diagnostics.Debug.WriteLine("failed to recv data in HandshakeReceive2Callback " + reconnectTimesRemain.ToString() + "/" + reconnectTimes.ToString());
                    this.Close();
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }
        private void Connect()
        {
            remote = null;
            remoteUDP = null;
            closed = false;
            arTCPConnection = null;
            arTCPRemote = null;
            if (select_server == null)
            {
                if (targetURI == null)
                {
                    targetURI = GetQueryString();
                    server = this.getCurrentServer(targetURI, true);
                }
                else
                {
                    server = this.getCurrentServer(targetURI, true, forceRandom);
                }
            }
            else
            {
                server = select_server;
            }
            speedTester.server = server.server;

            if (socks5RemotePort > 0)
            {
                if (server.udp_over_tcp)
                {
                    command = 1;
                }
            }
            ResetTimeout(TTL);

            lock (this)
            {
                server.ServerSpeedLog().AddConnectTimes();
                if (this.State == ConnectState.HANDSHAKE)
                {
                    this.State = ConnectState.CONNECTING;
                }
                try
                {
                    encryptor = EncryptorFactory.GetEncryptor(server.method, server.password);
                    encryptorUDP = EncryptorFactory.GetEncryptor(server.method, server.password);
                    server.GetConnections().AddRef(this.connection);
                }
                catch
                {

                }
            }
            this.protocol = ObfsFactory.GetObfs(server.protocol);
            this.obfs = ObfsFactory.GetObfs(server.obfs);
            {
                IPAddress ipAddress;
                string serverURI = server.server;
                int serverPort = server.server_port;
                if (socks5RemotePort > 0)
                {
                    serverURI = socks5RemoteHost;
                    serverPort = socks5RemotePort;
                }
                bool parsed = IPAddress.TryParse(serverURI, out ipAddress);
                if (!parsed)
                {
                    //IPHostEntry ipHostInfo = Dns.GetHostEntry(serverURI);
                    //ipAddress = ipHostInfo.AddressList[0];
                    if (server.DnsBuffer().isExpired(serverURI))
                    {
                        IAsyncResult result = Dns.BeginGetHostEntry(serverURI, new AsyncCallback(DnsCallback), new CallbackStatus());
                        double t = TTL;
                        if (t <= 0) t = 20;
                        else if (t <= 5) t = 5;
                        bool success = result.AsyncWaitHandle.WaitOne((int)(t * 250), true);
                        if (!success)
                        {
                            ((CallbackStatus)result.AsyncState).SetIfEqu(-1, 0);
                            if (((CallbackStatus)result.AsyncState).Status == -1)
                            {
                                lastErrCode = 8;
                                server.ServerSpeedLog().AddTimeoutTimes();
                                Close();
                            }
                        }
                        return;
                    }
                    else
                    {
                        ipAddress = server.DnsBuffer().ip;
                    }
                }
                BeginConnect(ipAddress, serverPort);
            }
        }

        private void StartConnect(IAsyncResult ar)
        {
            try
            {
                connection.EndSend(ar);
                Connect();
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            if (ar != null && ar.AsyncState != null)
                ((CallbackStatus)ar.AsyncState).SetIfEqu(1, 0);
            if (closed ||
                ar != null && ar.AsyncState != null && ((CallbackStatus)ar.AsyncState).Status != 1)
            {
                return;
            }
            try
            {
                // Complete the connection.
                if (socks5RemotePort != 0)
                {
                    remote.EndConnect(ar);
                }
                if (socks5RemotePort > 0)
                {
                    remoteTCPEndPoint = null;
                    if (ConnectProxyServer(server.server, server.server_port, remote, (int)SocketError.ConnectionReset))
                    {
                    }
                    else
                    {
                        throw new SocketException((int)SocketError.ConnectionReset);
                    }
                }
                speedTester.EndConnect();
                //server.ServerSpeedLog().AddConnectTime((int)(speedTester.timeConnectEnd - speedTester.timeConnectBegin).TotalMilliseconds);

                //Console.WriteLine("Socket connected to {0}",
                //    remote.RemoteEndPoint.ToString());

                ConnectState _state = this.State;
                if (_state == ConnectState.CONNECTING)
                {
                    StartPipe();
                }
                else if (_state == ConnectState.CONNECTED)
                {
                    //ERROR
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        public static byte[] ParseUDPHeader(byte[] buffer, ref int len)
        {
            if (buffer.Length == 0)
                return buffer;
            if (buffer[0] == 0x81)
            {
                len = len - 1;
                byte[] ret = new byte[len];
                Array.Copy(buffer, 1, ret, 0, len);
                return ret;
            }
            if (buffer[0] == 0x80 && len >= 2)
            {
                int ofbs_len = buffer[1];
                if (ofbs_len + 2 < len)
                {
                    len = len - ofbs_len - 2;
                    byte[] ret = new byte[len];
                    Array.Copy(buffer, ofbs_len + 2, ret, 0, len);
                    return ret;
                }
            }
            if (buffer[0] == 0x82 && len >= 3)
            {
                int ofbs_len = (buffer[1] << 8) + buffer[2];
                if (ofbs_len + 3 < len)
                {
                    len = len - ofbs_len - 3;
                    byte[] ret = new byte[len];
                    Array.Copy(buffer, ofbs_len + 3, ret, 0, len);
                    return ret;
                }
            }
            if (len < buffer.Length)
            {
                byte[] ret = new byte[len];
                Array.Copy(buffer, ret, len);
                return ret;
            }
            return buffer;
        }

        // do/end xxx tcp/udp Recv
        private void doConnectionTCPRecv()
        {
            if (connection != null && connectionTCPIdle)
            {
                connectionTCPIdle = false;
                arTCPConnection = connection.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(PipeConnectionReceiveCallback), null);
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
                const int RecvSize = 65536;

                IPEndPoint sender = new IPEndPoint(connectionUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                EndPoint tempEP = (EndPoint)sender;
                connectionUDPIdle = false;
                arUDPConnection = connectionUDP.BeginReceiveFrom(connetionRecvBuffer, 0, RecvSize, SocketFlags.None, ref tempEP,
                    new AsyncCallback(PipeConnectionUDPReceiveCallback), null);
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
                arTCPRemote = remote.BeginReceive(remoteRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(PipeRemoteReceiveCallback), null);
            }
        }

        private int endRemoteTCPRecv(IAsyncResult ar)
        {
            if (remote != null)
            {
                int bytesRead = remote.EndReceive(ar);
                remoteTCPIdle = true;
                return bytesRead;
            }
            return 0;
        }

        private void doRemoteUDPRecv()
        {
            if (remoteUDP != null && remoteUDPIdle)
            {
                const int RecvSize = 65536;

                IPEndPoint sender = new IPEndPoint(remoteUDP.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                EndPoint tempEP = (EndPoint)sender;
                remoteUDPIdle = false;
                arUDPRemote = remoteUDP.BeginReceiveFrom(remoteRecvBuffer, 0, RecvSize, SocketFlags.None, ref tempEP,
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

        private void SetObfsPlugin()
        {
            lock (obfsLock)
            {
                if (server.getProtocolData() == null)
                {
                    server.setProtocolData(protocol.InitData());
                }
                if (server.getObfsData() == null)
                {
                    server.setObfsData(obfs.InitData());
                }
            }
            int mss = 1440;
            if (remote != null)
            {
                try
                {
                    //mss = (int)remote.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.IpTimeToLive /* == TCP_MAXSEG */);
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e);
                }
            }
            int head_len;
            if (connectionSendBufferList != null && connectionSendBufferList.Count > 0)
            {
                head_len = ObfsBase.GetHeadSize(connectionSendBufferList[0], 30);
            }
            else
            {
                head_len = ObfsBase.GetHeadSize(remoteHeaderSendBuffer, 30);
            }
            if (remoteTCPEndPoint != null)
            {
                try
                {
                    protocol.SetServerInfo(new ServerInfo(remoteTCPEndPoint.Address.ToString(), server.server_port, "", server.getProtocolData(),
                        encryptor.getIV(), encryptor.getKey(), head_len, mss));
                }
                catch (Exception)
                {
                    protocol.SetServerInfo(new ServerInfo(server.server, server.server_port, "", server.getProtocolData(),
                        encryptor.getIV(), encryptor.getKey(), head_len, mss));
                }
                try
                {
                    obfs.SetServerInfo(new ServerInfo(remoteTCPEndPoint.Address.ToString(), server.server_port, server.obfsparam, server.getObfsData(),
                        encryptor.getIV(), encryptor.getKey(), head_len, mss));
                }
                catch (Exception)
                {
                    obfs.SetServerInfo(new ServerInfo(server.server, server.server_port, server.obfsparam, server.getObfsData(),
                        encryptor.getIV(), encryptor.getKey(), head_len, mss));
                }
            }
            else
            {
                protocol.SetServerInfo(new ServerInfo(server.server, server.server_port, "", server.getProtocolData(),
                    encryptor.getIV(), encryptor.getKey(), head_len, mss));
                obfs.SetServerInfo(new ServerInfo(server.server, server.server_port, server.obfsparam, server.getObfsData(),
                    encryptor.getIV(), encryptor.getKey(), head_len, mss));
            }
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

        // 2 sides connection start
        private void StartPipe()
        {
            if (closed)
            {
                return;
            }
            try
            {
                // set mark
                connectionTCPIdle = true;
                connectionUDPIdle = true;
                remoteTCPIdle = true;
                remoteUDPIdle = true;

                remoteUDPRecvBufferLength = 0;
                SetObfsPlugin();

                ResetTimeout(TTL);

                speedTester.BeginUpload();

                // remote ready
                if (connectionUDP == null) // TCP
                {
                    if (connectionSendBufferList != null && connectionSendBufferList.Count > 0)
                    {
                        foreach (byte[] buffer in connectionSendBufferList)
                        {
                            RemoteSend(buffer, buffer.Length);
                        }
                    }
                    else if (httpProxyState != null)
                    {
                        if (remoteHeaderSendBuffer != null)
                        {
                            detector.OnSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                            byte[] data = new byte[remoteHeaderSendBuffer.Length];
                            Array.Copy(remoteHeaderSendBuffer, data, data.Length);
                            connectionSendBufferList.Add(data);
                            RemoteSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                        }

                        //if (httpProxyState.httpProxy)
                        //{
                        //    byte[] buffer = new byte[0];
                        //    int buffer_len = 0;
                        //    httpProxyState.ParseHttpRequest(buffer, ref buffer_len);
                        //    if (remoteHeaderSendBuffer != null && remoteHeaderSendBuffer.Length > 0)
                        //    {
                        //        RemoteSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                        //        byte[] data = new byte[remoteHeaderSendBuffer.Length];
                        //        Array.Copy(remoteHeaderSendBuffer, data, data.Length);
                        //        connectionSendBufferList.Add(data);
                        //    }
                        //}

                        remoteHeaderSendBuffer = null;
                        //if (!httpProxyState.httpProxy)
                        {
                            httpProxyState = null;
                        }
                    }
                    else if (remoteHeaderSendBuffer != null)
                    {
                        detector.OnSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                        byte[] data = new byte[remoteHeaderSendBuffer.Length];
                        Array.Copy(remoteHeaderSendBuffer, data, data.Length);
                        connectionSendBufferList.Add(data);
                        RemoteSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                        remoteHeaderSendBuffer = null;
                    }
                }
                else // UDP
                {
                    const int RecvSize = 65536;
                    const int BufferSize = RecvSize + 1024;
                    connetionRecvBuffer = new byte[RecvSize * 2];
                    connetionSendBuffer = new byte[BufferSize * 2];
                    remoteRecvBuffer = new byte[RecvSize * 2];
                    remoteSendBuffer = new byte[BufferSize * 2];
                    if (
                        !server.udp_over_tcp &&
                        remoteUDP != null)
                    {
                        if (socks5RemotePort == 0)
                            CloseSocket(ref remote);
                        remoteHeaderSendBuffer = null;
                    }
                    else
                    {
                        if (remoteHeaderSendBuffer != null)
                        {
                            RemoteSend(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length);
                            remoteHeaderSendBuffer = null;
                        }
                    }
                }
                this.State = ConnectState.CONNECTED;

                // remote recv first
                doRemoteTCPRecv();
                doRemoteUDPRecv();

                // connection recv last
                doConnectionTCPRecv();
                doConnectionUDPRecv();
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void ConnectionSend(byte[] buffer, int bytesToSend)
        {
            if (connectionUDP == null)
                connection.BeginSend(buffer, 0, bytesToSend, 0, new AsyncCallback(PipeConnectionSendCallback), null);
            else
                connectionUDP.BeginSendTo(buffer, 0, bytesToSend, SocketFlags.None, connectionUDPEndPoint, new AsyncCallback(PipeConnectionUDPSendCallback), null);
        }
        // end ReceiveCallback
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

                if (bytesRead > 0)
                {
                    int bytesToSend = 0;
                    int obfsRecvSize;
                    bool needsendback;
                    byte[] remoteRecvObfsBuffer = obfs.ClientDecode(remoteRecvBuffer, bytesRead, out obfsRecvSize, out needsendback);
                    byte[] remoteSendBuffer = new byte[remoteRecvObfsBuffer.Length];
                    if (needsendback)
                    {
                        RemoteSend(connetionRecvBuffer, 0);
                        doRemoteTCPRecv();
                    }
                    if (obfsRecvSize > 0)
                    {
                        lock (decryptionLock)
                        {
                            encryptor.Decrypt(remoteRecvObfsBuffer, obfsRecvSize, remoteSendBuffer, out bytesToSend);
                            int outlength;
                            remoteSendBuffer = this.protocol.ClientPostDecrypt(remoteSendBuffer, bytesToSend, out outlength);
                            bytesToSend = outlength;
                        }
                    }
                    if (speedTester.BeginDownload())
                    {
                        int pingTime = -1;
                        if (speedTester.timeBeginDownload != null && speedTester.timeBeginUpload != null)
                            pingTime = (int)(speedTester.timeBeginDownload - speedTester.timeBeginUpload).TotalMilliseconds;
                        if (pingTime >= 0)
                            server.ServerSpeedLog().AddConnectTime(pingTime);
                    }

                    ResetTimeout(TTL);

                    if (bytesToSend == 0)
                    {
                        server.ServerSpeedLog().AddDownloadBytes(bytesRead);
                        speedTester.AddDownloadSize(bytesRead);

                        doRemoteTCPRecv();
                        return;
                    }
                    if (connectionUDP == null)
                        Logging.LogBin(LogLevel.Debug, "remote recv", remoteSendBuffer, bytesToSend);
                    else
                        Logging.LogBin(LogLevel.Debug, "udp remote recv", remoteSendBuffer, bytesToSend);

                    if (connectionUDP == null)
                    {
                        if (detector.OnRecv(remoteSendBuffer, bytesToSend) > 0)
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
                        connection.BeginSend(remoteSendBuffer, 0, bytesToSend, 0, new AsyncCallback(PipeConnectionSendCallback), null);
                        server.ServerSpeedLog().AddDownloadRawBytes(bytesToSend);
                        speedTester.AddRecvSize(bytesToSend);
                    }
                    else
                    {
                        List<byte[]> buffer_list = new List<byte[]>();
                        lock (recvUDPoverTCPLock)
                        {
                            if (bytesToSend + remoteUDPRecvBufferLength > remoteUDPRecvBuffer.Length)
                            {
                                Array.Resize(ref remoteUDPRecvBuffer, bytesToSend + remoteUDPRecvBufferLength);
                            }
                            Array.Copy(remoteSendBuffer, 0, remoteUDPRecvBuffer, remoteUDPRecvBufferLength, bytesToSend);
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
                                server.ServerSpeedLog().AddDownloadRawBytes(buffer.Length);
                                speedTester.AddRecvSize(buffer.Length);
                                connectionUDP.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, connectionUDPEndPoint, new AsyncCallback(PipeConnectionUDPSendCallback), null);
                            }
                        }
                    }
                    server.ServerSpeedLog().AddDownloadBytes(bytesRead);
                    speedTester.AddDownloadSize(bytesRead);
                }
                else
                {
                    final_close = true;
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
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

        private bool RemoveRemoteUDPRecvBufferHeader(ref int bytesRead)
        {
            if (socks5RemotePort > 0)
            {
                if (bytesRead < 7)
                {
                    return false;
                }
                int port = -1;
                if (remoteRecvBuffer[3] == 1)
                {
                    int head = 3 + 1 + 4 + 2;
                    bytesRead = bytesRead - head;
                    port = remoteRecvBuffer[head - 2] * 0x100 + remoteRecvBuffer[head - 1];
                    Array.Copy(remoteRecvBuffer, head, remoteRecvBuffer, 0, bytesRead);
                }
                else if (remoteRecvBuffer[3] == 4)
                {
                    int head = 3 + 1 + 16 + 2;
                    bytesRead = bytesRead - head;
                    port = remoteRecvBuffer[head - 2] * 0x100 + remoteRecvBuffer[head - 1];
                    Array.Copy(remoteRecvBuffer, head, remoteRecvBuffer, 0, bytesRead);
                }
                else if (remoteRecvBuffer[3] == 3)
                {
                    int head = 3 + 1 + 1 + remoteRecvBuffer[4] + 2;
                    bytesRead = bytesRead - head;
                    port = remoteRecvBuffer[head - 2] * 0x100 + remoteRecvBuffer[head - 1];
                    Array.Copy(remoteRecvBuffer, head, remoteRecvBuffer, 0, bytesRead);
                }
                else
                {
                    return false;
                }
                if (port != server.server_port && port != server.server_udp_port)
                {
                    return false;
                }
            }
            return true;
        }

        private void AddRemoteUDPRecvBufferHeader(byte[] decryptBuffer, ref int bytesToSend)
        {
            Array.Copy(decryptBuffer, 0, remoteSendBuffer, 3, bytesToSend);
            remoteSendBuffer[0] = 0;
            remoteSendBuffer[1] = 0;
            remoteSendBuffer[2] = 0;
            bytesToSend += 3;
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

                if (bytesRead > 0)
                {
                    server.ServerSpeedLog().AddDownloadBytes(bytesRead);
                    speedTester.AddDownloadSize(bytesRead);

                    int bytesToSend;
                    if (!RemoveRemoteUDPRecvBufferHeader(ref bytesRead))
                    {
                        return; // drop
                    }
                    lock (decryptionLock)
                    {
                        byte[] decryptBuffer = new byte[RecvSize * 2];
                        encryptorUDP.ResetDecrypt();
                        encryptorUDP.Decrypt(remoteRecvBuffer, bytesRead, decryptBuffer, out bytesToSend);
                        decryptBuffer = ParseUDPHeader(decryptBuffer, ref bytesToSend);
                        AddRemoteUDPRecvBufferHeader(decryptBuffer, ref bytesToSend);
                    }
                    if (connectionUDP == null)
                        Logging.LogBin(LogLevel.Debug, "remote recv", remoteSendBuffer, bytesToSend);
                    else
                        Logging.LogBin(LogLevel.Debug, "udp remote recv", remoteSendBuffer, bytesToSend);

                    int obfsSendSize;
                    byte[] obfsBuffer = this.protocol.ClientUdpPostDecrypt(remoteSendBuffer, bytesToSend, out obfsSendSize);
                    if (connectionUDP == null)
                        connection.BeginSend(obfsBuffer, 0, obfsSendSize, 0, new AsyncCallback(PipeConnectionSendCallback), null);
                    else
                        connectionUDP.BeginSendTo(remoteSendBuffer, 0, obfsSendSize, SocketFlags.None, connectionUDPEndPoint, new AsyncCallback(PipeConnectionUDPSendCallback), null);
                    speedTester.AddRecvSize(obfsSendSize);
                    server.ServerSpeedLog().AddDownloadRawBytes(obfsSendSize);
                    ResetTimeout(TTL);
                }
                else
                {
                    final_close = true;
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
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

        private void RemoteSend(byte[] bytes, int length)
        {
            if (remote == null)
                return;
            int bytesToSend = 0;
            {
                Logging.LogBin(LogLevel.Debug, "remote send", bytes, length);
                lock (encryptionLock)
                {
                    if (closed)
                    {
                        return;
                    }
                    int outlength;
                    byte[] bytesToEncrypt = this.protocol.ClientPreEncrypt(bytes, length, out outlength);
                    encryptor.Encrypt(bytesToEncrypt, outlength, connetionSendBuffer, out bytesToSend);
                }
            }
            int obfsSendSize;
            byte[] obfsBuffer = this.obfs.ClientEncode(connetionSendBuffer, bytesToSend, out obfsSendSize);
            if (remote != null && obfsSendSize > 0)
            {
                remote.BeginSend(obfsBuffer, 0, obfsSendSize, 0, new AsyncCallback(PipeRemoteSendCallback), null);
                server.ServerSpeedLog().AddUploadBytes(obfsSendSize);
                speedTester.AddUploadSize(obfsSendSize);
            }
        }

        private void RemoteSendto(byte[] bytes, int length)
        {
            int bytesToSend;
            byte[] bytesToEncrypt = null;
            int bytes_beg = 3;
            length -= bytes_beg;

            bytesToEncrypt = new byte[length];
            Array.Copy(bytes, bytes_beg, bytesToEncrypt, 0, length);
            lock (encryptionLock)
            {
                if (closed)
                {
                    return;
                }
                encryptorUDP.ResetEncrypt();
                this.protocol.SetServerInfoIV(encryptorUDP.getIV());
                int obfsSendSize;
                byte[] obfsBuffer = this.protocol.ClientUdpPreEncrypt(bytesToEncrypt, length, out obfsSendSize);
                encryptorUDP.Encrypt(obfsBuffer, obfsSendSize, connetionSendBuffer, out bytesToSend);
            }

            if (this.socks5RemotePort > 0)
            {
                IPAddress ipAddress;
                string serverURI = server.server;
                int serverPort = server.server_udp_port == 0 ? server.server_port : server.server_udp_port;
                bool parsed = IPAddress.TryParse(serverURI, out ipAddress);
                if (!parsed)
                {
                    bytesToEncrypt = new byte[bytes_beg + 1 + 1 + serverURI.Length + 2 + bytesToSend];
                    Array.Copy(connetionSendBuffer, 0, bytesToEncrypt, bytes_beg + 1 + 1 + serverURI.Length + 2, bytesToSend);
                    bytesToEncrypt[0] = 0;
                    bytesToEncrypt[1] = 0;
                    bytesToEncrypt[2] = 0;
                    bytesToEncrypt[3] = (byte)3;
                    bytesToEncrypt[4] = (byte)serverURI.Length;
                    for (int i = 0; i < serverURI.Length; ++i)
                    {
                        bytesToEncrypt[5 + i] = (byte)serverURI[i];
                    }
                    bytesToEncrypt[5 + serverURI.Length] = (byte)(serverPort / 0x100);
                    bytesToEncrypt[5 + serverURI.Length + 1] = (byte)(serverPort % 0x100);
                }
                else
                {
                    byte[] addBytes = ipAddress.GetAddressBytes();
                    bytesToEncrypt = new byte[bytes_beg + 1 + addBytes.Length + 2 + bytesToSend];
                    Array.Copy(connetionSendBuffer, 0, bytesToEncrypt, bytes_beg + 1 + addBytes.Length + 2, bytesToSend);
                    bytesToEncrypt[0] = 0;
                    bytesToEncrypt[1] = 0;
                    bytesToEncrypt[2] = 0;
                    bytesToEncrypt[3] = ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? (byte)4 : (byte)1;
                    for (int i = 0; i < addBytes.Length; ++i)
                    {
                        bytesToEncrypt[4 + i] = addBytes[i];
                    }
                    bytesToEncrypt[4 + addBytes.Length] = (byte)(serverPort / 0x100);
                    bytesToEncrypt[4 + addBytes.Length + 1] = (byte)(serverPort % 0x100);
                }

                bytesToSend = bytesToEncrypt.Length;
                Array.Copy(bytesToEncrypt, connetionSendBuffer, bytesToSend);
            }
            remoteUDP.BeginSendTo(connetionSendBuffer, 0, bytesToSend, 0, remoteUDPEndPoint, new AsyncCallback(PipeRemoteUDPSendCallback), null);
            server.ServerSpeedLog().AddUploadBytes(bytesToSend);
            speedTester.AddUploadSize(bytesToSend);
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
                        ResetTimeout(TTL);
                        return;
                    }
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
                    if (obfs != null && obfs.getSentLength() > 0)
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

                    ResetTimeout(TTL);
                    RemoteSend(connetionRecvBuffer, bytesRead);
                }
                else
                {
                    final_close = true;
                }
            }
            catch (Exception e)
            {
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
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
                    Array.Copy(connetionRecvBuffer, connetionSendBuffer, bytesRead);
                    Logging.LogBin(LogLevel.Debug, "udp remote send", connetionRecvBuffer, bytesRead);
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
                                Array.Copy(connetionRecvBuffer, 0, connetionSendBuffer2, 1, bytesRead);
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
                    ResetTimeout(TTL);
                }
                else
                {
                    final_close = true;
                }
            }
            catch (Exception e)
            {
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
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

        // end SendCallback
        private void PipeRemoteSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                remote.EndSend(ar);
                doConnectionTCPRecv();
                doConnectionUDPRecv();
                if (lastKeepTime == null || (DateTime.Now - lastKeepTime).TotalSeconds > 5)
                {
                    if (keepCurrentServer != null) keepCurrentServer(targetURI);
                    lastKeepTime = DateTime.Now;
                }
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
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
                doConnectionTCPRecv();
                doConnectionUDPRecv();
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void PipeRemoteTDPSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                //remoteTDP.EndSendTo(ar);
                doConnectionTCPRecv();
                doConnectionUDPRecv();
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
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
                doRemoteTCPRecv();
                doRemoteUDPRecv();
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
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
                doRemoteTCPRecv();
                doRemoteUDPRecv();
            }
            catch (Exception e)
            {
                LogSocketException(e);
                string remarks;
                string server_url = getServerUrl(out remarks);
                if (!Logging.LogSocketException(remarks, server_url, e))
                    Logging.LogUsefulException(e);
                this.Close();
            }
        }
    }

}
