using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shadowsocks.Encryption;
using Shadowsocks.Model;
using Shadowsocks.Obfs;

namespace Shadowsocks.Controller
{
    public class CallbackState
    {
        public byte[] buffer;
        public int size;
        public int protocol_size;
        public object state;
    }

    public class ProxySocketTun
    {
        protected Socket _socket;
        protected EndPoint _socketEndPoint;
        protected IPEndPoint _remoteUDPEndPoint;

        protected bool _proxy;
        protected string _proxy_server;
        protected int _proxy_udp_port;

        protected const int RecvSize = 2048;

        private byte[] SendEncryptBuffer = new byte[RecvSize];
        private byte[] ReceiveDecryptBuffer = new byte[RecvSize * 2];

        protected bool _close;

        public ProxySocketTun(Socket socket)
        {
            _socket = socket;
        }

        public ProxySocketTun(AddressFamily af, SocketType type, ProtocolType protocol)
        {
            _socket = new Socket(af, type, protocol);
        }

        public Socket GetSocket()
        {
            return _socket;
        }

        public bool IsClose
        {
            get
            {
                return _close;
            }
        }

        public bool GoS5Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                _proxy = value;
            }
        }

        public AddressFamily AddressFamily
        {
            get
            {
                return _socket.AddressFamily;
            }
        }

        public void Shutdown(SocketShutdown how)
        {
            _socket.Shutdown(how);
        }

        public void Close()
        {
            _socket.Close();
        }

        public IAsyncResult BeginConnect(EndPoint ep, AsyncCallback callback, object state)
        {
            _close = false;
            _socketEndPoint = ep;
            return _socket.BeginConnect(ep, callback, state);
        }

        public void EndConnect(IAsyncResult ar)
        {
            _socket.EndConnect(ar);
        }

        public int Receive(byte[] buffer, int size, SocketFlags flags)
        {
            return _socket.Receive(buffer, size, SocketFlags.None);
        }

        public IAsyncResult BeginReceive(byte[] buffer, int size, SocketFlags flags, AsyncCallback callback, object state)
        {
            CallbackState st = new CallbackState();
            st.buffer = buffer;
            st.size = size;
            st.state = state;
            return _socket.BeginReceive(buffer, 0, size, flags, callback, st);
        }

        public int EndReceive(IAsyncResult ar)
        {
            int bytesRead = _socket.EndReceive(ar);
            if (bytesRead > 0)
            {
                CallbackState st = (CallbackState)ar.AsyncState;
                st.size = bytesRead;
                return bytesRead;
            }
            else
            {
                _close = true;
            }
            return bytesRead;
        }

        public virtual int Send(byte[] buffer, int size, SocketFlags flags)
        {
            _socket.Send(buffer, size, 0);
            return size;
        }

        public int BeginSend(byte[] buffer, int size, SocketFlags flags, AsyncCallback callback, object state)
        {
            CallbackState st = new CallbackState();
            st.size = size;
            st.state = state;

            _socket.BeginSend(buffer, 0, size, 0, callback, st);
            return size;
        }

        public int EndSend(IAsyncResult ar)
        {
            return _socket.EndSend(ar);
        }

        public IAsyncResult BeginReceiveFrom(byte[] buffer, int size, SocketFlags flags, ref EndPoint ep, AsyncCallback callback, object state)
        {
            CallbackState st = new CallbackState();
            st.buffer = buffer;
            st.size = size;
            st.state = state;
            return _socket.BeginReceiveFrom(buffer, 0, size, flags, ref ep, callback, st);
        }

        public int GetAsyncResultSize(IAsyncResult ar)
        {
            CallbackState st = (CallbackState)ar.AsyncState;
            return st.size;
        }

        public byte[] GetAsyncResultBuffer(IAsyncResult ar)
        {
            CallbackState st = (CallbackState)ar.AsyncState;
            return st.buffer;
        }

        public bool ConnectSocks5ProxyServer(string strRemoteHost, int iRemotePort, bool udp, string socks5RemoteUsername, string socks5RemotePassword)
        {
            int socketErrorCode = (int)SocketError.ConnectionReset;
            _proxy = true;

            //构造Socks5代理服务器第一连接头(无用户名密码)
            byte[] bySock5Send = new Byte[10];
            bySock5Send[0] = 5;
            bySock5Send[1] = 2;
            bySock5Send[2] = 0;
            bySock5Send[3] = 2;

            //发送Socks5代理第一次连接信息
            _socket.Send(bySock5Send, 4, SocketFlags.None);

            byte[] bySock5Receive = new byte[32];
            int iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

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
                        _socket.Send(bySock5Send, bySock5Send.Length, SocketFlags.None);
                        iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

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
            if (!udp) // TCP
            {
                List<byte> dataSock5Send = new List<byte>();
                dataSock5Send.Add(5);
                dataSock5Send.Add(1);
                dataSock5Send.Add(0);

                IPAddress ipAdd;
                //bool ForceRemoteDnsResolve = false;
                bool parsed = IPAddress.TryParse(strRemoteHost, out ipAdd);
                //if (!parsed && !ForceRemoteDnsResolve)
                //{
                //    if (server.DnsTargetBuffer().isExpired(strRemoteHost))
                //    {
                //        try
                //        {
                //            IPHostEntry ipHostInfo = Dns.GetHostEntry(strRemoteHost);
                //            ipAdd = ipHostInfo.AddressList[0];
                //            server.DnsTargetBuffer().UpdateDns(strRemoteHost, ipAdd);
                //        }
                //        catch (Exception)
                //        {
                //        }
                //    }
                //    else
                //    {
                //        ipAdd = server.DnsTargetBuffer().ip;
                //    }
                //}
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

                _socket.Send(dataSock5Send.ToArray(), dataSock5Send.Count, SocketFlags.None);
                iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

                if (iRecCount < 2 || bySock5Receive[0] != 5 || bySock5Receive[1] != 0)
                {
                    throw new SocketException(socketErrorCode);
                    //throw new Exception("第二次连接Socks5代理返回数据出错。");
                }
                return true;
            }
            else // UDP
            {
                List<byte> dataSock5Send = new List<byte>();
                dataSock5Send.Add(5);
                dataSock5Send.Add(3);
                dataSock5Send.Add(0);

                IPAddress ipAdd = ((IPEndPoint)_socketEndPoint).Address;
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

                _socket.Send(dataSock5Send.ToArray(), dataSock5Send.Count, SocketFlags.None);
                iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

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
                    _remoteUDPEndPoint = new IPEndPoint(ipAdd, port);
                }
                return true;
            }
        }

        public void SetTcpServer(string server, int port)
        {
            _proxy_server = server;
            _proxy_udp_port = port;
        }

        public void SetUdpServer(string server, int port)
        {
            _proxy_server = server;
            _proxy_udp_port = port;
        }

        public void SetUdpEndPoint(IPEndPoint ep)
        {
            _remoteUDPEndPoint = ep;
        }

        public bool ConnectHttpProxyServer(string strRemoteHost, int iRemotePort, string socks5RemoteUsername, string socks5RemotePassword, string proxyUserAgent)
        {
            _proxy = true;

            IPAddress ipAdd;
            //bool ForceRemoteDnsResolve = true;
            bool parsed = IPAddress.TryParse(strRemoteHost, out ipAdd);
            //if (!parsed && !ForceRemoteDnsResolve)
            //{
            //    if (server.DnsTargetBuffer().isExpired(strRemoteHost))
            //    {
            //        try
            //        {
            //            IPHostEntry ipHostInfo = Dns.GetHostEntry(strRemoteHost);
            //            ipAdd = ipHostInfo.AddressList[0];
            //            server.DnsTargetBuffer().UpdateDns(strRemoteHost, ipAdd);
            //        }
            //        catch (Exception)
            //        {
            //        }
            //    }
            //    else
            //    {
            //        ipAdd = server.DnsTargetBuffer().ip;
            //    }
            //}
            if (ipAdd != null)
            {
                strRemoteHost = ipAdd.ToString();
            }
            string host = (strRemoteHost.IndexOf(':') >= 0 ? "[" + strRemoteHost + "]" : strRemoteHost) + ":" + iRemotePort.ToString();
            string authstr = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(socks5RemoteUsername + ":" + socks5RemotePassword));
            string cmd = "CONNECT " + host + " HTTP/1.0\r\n"
                + "Host: " + host + "\r\n";
            if (proxyUserAgent != null && proxyUserAgent.Length > 0)
                cmd += "User-Agent: " + proxyUserAgent + "\r\n";
            cmd += "Proxy-Connection: Keep-Alive\r\n";
            if (socks5RemoteUsername.Length > 0)
                cmd += "Proxy-Authorization: Basic " + authstr + "\r\n";
            cmd += "\r\n";
            byte[] httpData = System.Text.Encoding.UTF8.GetBytes(cmd);
            _socket.Send(httpData, httpData.Length, SocketFlags.None);
            byte[] byReceive = new byte[1024];
            int iRecCount = _socket.Receive(byReceive, byReceive.Length, SocketFlags.None);
            if (iRecCount > 13)
            {
                string data = System.Text.Encoding.UTF8.GetString(byReceive, 0, iRecCount);
                string[] data_part = data.Split(' ');
                if (data_part.Length > 1 && data_part[1] == "200")
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class ProxySocketTunLocal : ProxySocketTun
    {
        public string local_sendback_protocol;

        public ProxySocketTunLocal(Socket socket)
            : base(socket)
        {
            _socket = socket;
        }

        public ProxySocketTunLocal(AddressFamily af, SocketType type, ProtocolType protocol)
            : base(af, type, protocol)
        {

        }

        public override int Send(byte[] buffer, int size, SocketFlags flags)
        {
            if (local_sendback_protocol != null)
            {
                if (local_sendback_protocol == "http")
                {
                    byte[] data = System.Text.Encoding.UTF8.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                    _socket.Send(data, data.Length, 0);
                }
                else if (local_sendback_protocol == "socks5")
                {
                    if (_socket.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] response = { 5, 0, 0, 1,
                                0, 0, 0, 0,
                                0, 0 };
                        _socket.Send(response);
                    }
                    else
                    {
                        byte[] response = { 5, 0, 0, 4,
                                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                0, 0 };
                        _socket.Send(response);
                    }
                }
                local_sendback_protocol = null;
            }
            _socket.Send(buffer, size, 0);
            return size;
        }

    }

    class ProxyEncryptSocket
    {
        protected Socket _socket;
        protected EndPoint _socketEndPoint;
        protected IPEndPoint _remoteUDPEndPoint;

        protected IEncryptor _encryptor;
        protected object _encryptionLock = new object();
        protected object _decryptionLock = new object();
        protected string _method;
        protected string _password;
        public IObfs _protocol;
        public IObfs _obfs;
        //protected object obfsLock = new object();
        protected bool _proxy;
        protected string _proxy_server;
        protected int _proxy_udp_port;

        //private bool header_sent = false;

        protected const int RecvSize = 2048;

        private byte[] SendEncryptBuffer = new byte[RecvSize];
        private byte[] ReceiveDecryptBuffer = new byte[RecvSize * 2];

        protected bool _close;

        public ProxyEncryptSocket(AddressFamily af, SocketType type, ProtocolType protocol)
        {
            _socket = new Socket(af, type, protocol);
        }

        public Socket GetSocket()
        {
            return _socket;
        }

        public bool IsClose
        {
            get
            {
                return _close;
            }
        }

        public bool GoS5Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                _proxy = value;
            }
        }

        public bool CanSendKeepAlive
        {
            get
            {
                return _protocol.isKeepAlive();
            }
        }

        public bool isProtocolSendback
        {
            get
            {
                return _protocol.isAlwaysSendback();
            }
        }

        public bool isObfsSendback
        {
            get
            {
                return _obfs.isAlwaysSendback();
            }
        }

        public AddressFamily AddressFamily
        {
            get
            {
                return _socket.AddressFamily;
            }
        }

        public void Shutdown(SocketShutdown how)
        {
            _socket.Shutdown(how);
        }

        public void Close()
        {
            _socket.Close();

            if (_protocol != null)
            {
                _protocol.Dispose();
                _protocol = null;
            }
            if (_obfs != null)
            {
                _obfs.Dispose();
                _obfs = null;
            }

            lock (_encryptionLock)
            {
                lock (_decryptionLock)
                {
                    if (_encryptor != null)
                        ((IDisposable)_encryptor).Dispose();
                }
            }
        }

        public IAsyncResult BeginConnect(EndPoint ep, AsyncCallback callback, object state)
        {
            _close = false;
            _socketEndPoint = ep;
            return _socket.BeginConnect(ep, callback, state);
        }

        public void EndConnect(IAsyncResult ar)
        {
            _socket.EndConnect(ar);
        }

        public void CreateEncryptor(string method, string password)
        {
            _encryptor = EncryptorFactory.GetEncryptor(method, password);
            _method = method;
            _password = password;
        }

        public void SetProtocol(IObfs protocol)
        {
            _protocol = protocol;
        }

        public void SetObfs(IObfs obfs)
        {
            _obfs = obfs;
        }

        public void SetObfsPlugin(Server server, int head_len)
        {
            lock (server) // need a server lock
            {
                if (server.getProtocolData() == null)
                {
                    server.setProtocolData(_protocol.InitData());
                }
                if (server.getObfsData() == null)
                {
                    server.setObfsData(_obfs.InitData());
                }
            }
            int mss = 1460;
            string server_addr = server.server;
            if (_proxy_server != null)
                server_addr = _proxy_server;
            _protocol.SetServerInfo(new ServerInfo(server_addr, server.server_port, server.protocolparam??"", server.getProtocolData(),
                _encryptor.getIV(), _password, _encryptor.getKey(), head_len, mss));
            _obfs.SetServerInfo(new ServerInfo(server_addr, server.server_port, server.obfsparam??"", server.getObfsData(),
                _encryptor.getIV(), _password, _encryptor.getKey(), head_len, mss));
        }

        public int Receive(byte[] recv_buffer, int size, SocketFlags flags, out int bytesRead, out int protocolSize, out bool sendback)
        {
            bytesRead = _socket.Receive(recv_buffer, size, flags);
            protocolSize = 0;
            if (bytesRead > 0)
            {
                lock (_decryptionLock)
                {
                    int bytesToSend = 0;
                    int obfsRecvSize;
                    byte[] remoteRecvObfsBuffer = _obfs.ClientDecode(recv_buffer, bytesRead, out obfsRecvSize, out sendback);
                    if (obfsRecvSize > 0)
                    {
                        Util.Utils.SetArrayMinSize(ref ReceiveDecryptBuffer, obfsRecvSize);
                        _encryptor.Decrypt(remoteRecvObfsBuffer, obfsRecvSize, ReceiveDecryptBuffer, out bytesToSend);
                        int outlength;
                        protocolSize = bytesToSend;
                        byte[] buffer = _protocol.ClientPostDecrypt(ReceiveDecryptBuffer, bytesToSend, out outlength);
                        //if (recv_buffer.Length < outlength) //ASSERT
                        Array.Copy(buffer, 0, recv_buffer, 0, outlength);
                        return outlength;
                    }
                }
                return 0;
            }
            else
            {
                sendback = false;
                _close = true;
            }
            return bytesRead;
        }

        public IAsyncResult BeginReceive(byte[] buffer, int size, SocketFlags flags, AsyncCallback callback, object state)
        {
            CallbackState st = new CallbackState();
            st.buffer = buffer;
            st.size = size;
            st.state = state;
            return _socket.BeginReceive(buffer, 0, size, flags, callback, st);
        }

        public int EndReceive(IAsyncResult ar, out bool sendback)
        {
            int bytesRead = _socket.EndReceive(ar);
            sendback = false;
            if (bytesRead > 0)
            {
                CallbackState st = (CallbackState)ar.AsyncState;
                st.size = bytesRead;

                lock (_decryptionLock)
                {
                    int bytesToSend = 0;
                    int obfsRecvSize;
                    byte[] remoteRecvObfsBuffer = _obfs.ClientDecode(st.buffer, bytesRead, out obfsRecvSize, out sendback);
                    if (obfsRecvSize > 0)
                    {
                        Util.Utils.SetArrayMinSize(ref ReceiveDecryptBuffer, obfsRecvSize);
                        _encryptor.Decrypt(remoteRecvObfsBuffer, obfsRecvSize, ReceiveDecryptBuffer, out bytesToSend);
                        int outlength;
                        st.protocol_size = bytesToSend;
                        byte[] buffer = _protocol.ClientPostDecrypt(ReceiveDecryptBuffer, bytesToSend, out outlength);
                        if (st.buffer.Length < outlength)
                        {
                            Array.Resize(ref st.buffer, outlength);
                        }
                        Array.Copy(buffer, 0, st.buffer, 0, outlength);
                        return outlength;
                    }
                }
                return 0;
            }
            else
            {
                _close = true;
            }
            return bytesRead;
        }

        public int Send(byte[] buffer, int size, SocketFlags flags)
        {
            int bytesToSend = 0;
            int obfsSendSize;
            int sendSize;
            byte[] obfsBuffer;

            lock (_encryptionLock)
            {
                int outlength;
                //if (!header_sent)
                //{
                //    header_sent = true;
                //    if (buffer[0] == 3 && _method == "none")
                //    {
                //        for (int i = 0; i < buffer[1]; ++i)
                //        {
                //            buffer[i + 2] |= 0x80;
                //        }
                //        buffer[0] = 2;
                //    }
                //}
                byte[] bytesToEncrypt = _protocol.ClientPreEncrypt(buffer, size, out outlength);
                Util.Utils.SetArrayMinSize(ref SendEncryptBuffer, outlength + 32);
                _encryptor.Encrypt(bytesToEncrypt, outlength, SendEncryptBuffer, out bytesToSend);
                obfsBuffer = _obfs.ClientEncode(SendEncryptBuffer, bytesToSend, out obfsSendSize);
                sendSize = _socket.Send(obfsBuffer, obfsSendSize, 0);
            }
            while (sendSize < obfsSendSize)
            {
                int new_size = _socket.Send(obfsBuffer, sendSize, obfsSendSize - sendSize, 0);
                sendSize += new_size;
            }
            return obfsSendSize;
        }

        public int BeginSend(byte[] buffer, int size, SocketFlags flags, AsyncCallback callback, object state)
        {
            CallbackState st = new CallbackState();
            st.size = size;
            st.state = state;

            int bytesToSend = 0;
            int obfsSendSize;
            byte[] obfsBuffer;
            lock (_encryptionLock)
            {
                int outlength;
                byte[] bytesToEncrypt = _protocol.ClientPreEncrypt(buffer, size, out outlength);
                Util.Utils.SetArrayMinSize(ref SendEncryptBuffer, outlength + 32);
                _encryptor.Encrypt(bytesToEncrypt, outlength, SendEncryptBuffer, out bytesToSend);
                obfsBuffer = _obfs.ClientEncode(SendEncryptBuffer, bytesToSend, out obfsSendSize);
                _socket.BeginSend(obfsBuffer, 0, obfsSendSize, 0, callback, st);
            }
            return obfsSendSize;
        }

        public int EndSend(IAsyncResult ar)
        {
            return _socket.EndSend(ar);
        }

        public IAsyncResult BeginReceiveFrom(byte[] buffer, int size, SocketFlags flags, ref EndPoint ep, AsyncCallback callback, object state)
        {
            CallbackState st = new CallbackState();
            st.buffer = buffer;
            st.size = size;
            st.state = state;
            return _socket.BeginReceiveFrom(buffer, 0, size, flags, ref ep, callback, st);
        }

        private bool RemoveRemoteUDPRecvBufferHeader(byte[] remoteRecvBuffer, ref int bytesRead)
        {
            if (_proxy)
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
                //if (port != server.server_port && port != server.server_udp_port)
                //{
                //    return false;
                //}
            }
            return true;
        }

        protected static byte[] ParseUDPHeader(byte[] buffer, ref int len)
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

        protected void AddRemoteUDPRecvBufferHeader(byte[] decryptBuffer, byte[] remoteSendBuffer, ref int bytesToSend)
        {
            Array.Copy(decryptBuffer, 0, remoteSendBuffer, 3, bytesToSend);
            remoteSendBuffer[0] = 0;
            remoteSendBuffer[1] = 0;
            remoteSendBuffer[2] = 0;
            bytesToSend += 3;
        }

        public int EndReceiveFrom(IAsyncResult ar, ref EndPoint ep)
        {
            int bytesRead = _socket.EndReceiveFrom(ar, ref ep);
            if (bytesRead > 0)
            {
                CallbackState st = (CallbackState)ar.AsyncState;
                st.size = bytesRead;

                int bytesToSend;
                if (!RemoveRemoteUDPRecvBufferHeader(st.buffer, ref bytesRead))
                {
                    return 0; // drop
                }
                byte[] remoteSendBuffer = new byte[65536];
                byte[] obfsBuffer;
                lock (_decryptionLock)
                {
                    byte[] decryptBuffer = new byte[65536];
                    _encryptor.ResetDecrypt();
                    _encryptor.Decrypt(st.buffer, bytesRead, decryptBuffer, out bytesToSend);
                    obfsBuffer = _protocol.ClientUdpPostDecrypt(decryptBuffer, bytesToSend, out bytesToSend);
                    decryptBuffer = ParseUDPHeader(obfsBuffer, ref bytesToSend);
                    AddRemoteUDPRecvBufferHeader(decryptBuffer, remoteSendBuffer, ref bytesToSend);
                }
                Array.Copy(remoteSendBuffer, 0, st.buffer, 0, bytesToSend);
                return bytesToSend;
            }
            else
            {
                _close = true;
            }
            return bytesRead;
        }

        public int BeginSendTo(byte[] buffer, int size, SocketFlags flags, AsyncCallback callback, object state)
        {
            CallbackState st = new CallbackState();
            st.buffer = buffer;
            st.size = size;
            st.state = state;

            int bytesToSend;
            byte[] bytesToEncrypt = null;
            byte[] connetionSendBuffer = new byte[65536];
            int bytes_beg = 3;
            int length = size - bytes_beg;

            bytesToEncrypt = new byte[length];
            Array.Copy(buffer, bytes_beg, bytesToEncrypt, 0, length);
            lock (_encryptionLock)
            {
                _encryptor.ResetEncrypt();
                _protocol.SetServerInfoIV(_encryptor.getIV());
                int obfsSendSize;
                byte[] obfsBuffer = _protocol.ClientUdpPreEncrypt(bytesToEncrypt, length, out obfsSendSize);
                _encryptor.Encrypt(obfsBuffer, obfsSendSize, connetionSendBuffer, out bytesToSend);
            }

            if (_proxy)
            {
                IPAddress ipAddress;
                string serverURI = _proxy_server;
                int serverPort = _proxy_udp_port;
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
            _socket.BeginSendTo(connetionSendBuffer, 0, bytesToSend, flags, _remoteUDPEndPoint, callback, st);
            return bytesToSend;
        }

        public int EndSendTo(IAsyncResult ar)
        {
            return _socket.EndSendTo(ar);
        }

        public int GetAsyncResultSize(IAsyncResult ar)
        {
            CallbackState st = (CallbackState)ar.AsyncState;
            return st.size;
        }

        public int GetAsyncProtocolSize(IAsyncResult ar)
        {
            CallbackState st = (CallbackState)ar.AsyncState;
            return st.protocol_size;
        }

        public byte[] GetAsyncResultBuffer(IAsyncResult ar)
        {
            CallbackState st = (CallbackState)ar.AsyncState;
            return st.buffer;
        }

        public IPEndPoint GetProxyUdpEndPoint()
        {
            return _remoteUDPEndPoint;
        }

        public bool ConnectSocks5ProxyServer(string strRemoteHost, int iRemotePort, bool udp, string socks5RemoteUsername, string socks5RemotePassword)
        {
            int socketErrorCode = (int)SocketError.ConnectionReset;
            _proxy = true;

            //构造Socks5代理服务器第一连接头(无用户名密码)
            byte[] bySock5Send = new Byte[10];
            bySock5Send[0] = 5;
            bySock5Send[1] = 2;
            bySock5Send[2] = 0;
            bySock5Send[3] = 2;

            //发送Socks5代理第一次连接信息
            _socket.Send(bySock5Send, 4, SocketFlags.None);

            byte[] bySock5Receive = new byte[32];
            int iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

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
                        _socket.Send(bySock5Send, bySock5Send.Length, SocketFlags.None);
                        iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

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
            if (!udp) // TCP
            {
                List<byte> dataSock5Send = new List<byte>();
                dataSock5Send.Add(5);
                dataSock5Send.Add(1);
                dataSock5Send.Add(0);

                IPAddress ipAdd;
                //bool ForceRemoteDnsResolve = false;
                bool parsed = IPAddress.TryParse(strRemoteHost, out ipAdd);
                //if (!parsed && !ForceRemoteDnsResolve)
                //{
                //    if (server.DnsTargetBuffer().isExpired(strRemoteHost))
                //    {
                //        try
                //        {
                //            IPHostEntry ipHostInfo = Dns.GetHostEntry(strRemoteHost);
                //            ipAdd = ipHostInfo.AddressList[0];
                //            server.DnsTargetBuffer().UpdateDns(strRemoteHost, ipAdd);
                //        }
                //        catch (Exception)
                //        {
                //        }
                //    }
                //    else
                //    {
                //        ipAdd = server.DnsTargetBuffer().ip;
                //    }
                //}
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

                _socket.Send(dataSock5Send.ToArray(), dataSock5Send.Count, SocketFlags.None);
                iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

                if (iRecCount < 2 || bySock5Receive[0] != 5 || bySock5Receive[1] != 0)
                {
                    throw new SocketException(socketErrorCode);
                    //throw new Exception("第二次连接Socks5代理返回数据出错。");
                }
                return true;
            }
            else // UDP
            {
                List<byte> dataSock5Send = new List<byte>();
                dataSock5Send.Add(5);
                dataSock5Send.Add(3);
                dataSock5Send.Add(0);

                IPAddress ipAdd = ((IPEndPoint)_socketEndPoint).Address;
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

                _socket.Send(dataSock5Send.ToArray(), dataSock5Send.Count, SocketFlags.None);
                iRecCount = _socket.Receive(bySock5Receive, bySock5Receive.Length, SocketFlags.None);

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
                    _remoteUDPEndPoint = new IPEndPoint(ipAdd, port);
                }
                return true;
            }
        }

        public void SetTcpServer(string server, int port)
        {
            _proxy_server = server;
            _proxy_udp_port = port;
        }

        public void SetUdpServer(string server, int port)
        {
            _proxy_server = server;
            _proxy_udp_port = port;
        }

        public void SetUdpEndPoint(IPEndPoint ep)
        {
            _remoteUDPEndPoint = ep;
        }

        public bool ConnectHttpProxyServer(string strRemoteHost, int iRemotePort, string socks5RemoteUsername, string socks5RemotePassword, string proxyUserAgent)
        {
            _proxy = true;

            IPAddress ipAdd;
            //bool ForceRemoteDnsResolve = true;
            bool parsed = IPAddress.TryParse(strRemoteHost, out ipAdd);
            //if (!parsed && !ForceRemoteDnsResolve)
            //{
            //    if (server.DnsTargetBuffer().isExpired(strRemoteHost))
            //    {
            //        try
            //        {
            //            IPHostEntry ipHostInfo = Dns.GetHostEntry(strRemoteHost);
            //            ipAdd = ipHostInfo.AddressList[0];
            //            server.DnsTargetBuffer().UpdateDns(strRemoteHost, ipAdd);
            //        }
            //        catch (Exception)
            //        {
            //        }
            //    }
            //    else
            //    {
            //        ipAdd = server.DnsTargetBuffer().ip;
            //    }
            //}
            if (ipAdd != null)
            {
                strRemoteHost = ipAdd.ToString();
            }
            string host = (strRemoteHost.IndexOf(':') >= 0 ? "[" + strRemoteHost + "]" : strRemoteHost) + ":" + iRemotePort.ToString();
            string authstr = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(socks5RemoteUsername + ":" + socks5RemotePassword));
            string cmd = "CONNECT " + host + " HTTP/1.0\r\n"
                + "Host: " + host + "\r\n";
            if (proxyUserAgent != null && proxyUserAgent.Length > 0)
                cmd += "User-Agent: " + proxyUserAgent + "\r\n";
            cmd += "Proxy-Connection: Keep-Alive\r\n";
            if (socks5RemoteUsername.Length > 0)
                cmd += "Proxy-Authorization: Basic " + authstr + "\r\n";
            cmd += "\r\n";
            byte[] httpData = System.Text.Encoding.UTF8.GetBytes(cmd);
            _socket.Send(httpData, httpData.Length, SocketFlags.None);
            byte[] byReceive = new byte[1024];
            int iRecCount = _socket.Receive(byReceive, byReceive.Length, SocketFlags.None);
            if (iRecCount > 13)
            {
                string data = System.Text.Encoding.UTF8.GetString(byReceive, 0, iRecCount);
                string[] data_part = data.Split(' ');
                if (data_part.Length > 1 && data_part[1] == "200")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
