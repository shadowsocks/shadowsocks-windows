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

    class ProxyAuthHandler
    {
        private Configuration _config;
        private ServerTransferTotal _transfer;


        private byte[] _firstPacket;
        private int _firstPacketLength;

        private Socket _connection;
        private Socket _connectionUDP;

        protected const int RECV_SIZE = 16384;
        protected byte[] _connetionRecvBuffer = new byte[RECV_SIZE * 2];

        public byte command;
        protected byte[] _remoteHeaderSendBuffer;

        protected HttpPraser httpProxyState;

        public ProxyAuthHandler(Configuration config, ServerTransferTotal transfer, byte[] firstPacket, int length, Socket socket)
        {
            int local_port = ((IPEndPoint)socket.LocalEndPoint).Port;

            _config = config;
            _transfer = transfer;
            _firstPacket = firstPacket;
            _firstPacketLength = length;
            _connection = socket;
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            if (_config.GetPortMapCache().ContainsKey(local_port))
            {
                Connect();
            }
            else
            {
                HandshakeReceive();
            }
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

        private void Close()
        {
            CloseSocket(ref _connection);
        }

        bool AuthConnection(Socket connection, string authUser, string authPass)
        {
            if ((_config.authUser ?? "").Length == 0)
            {
                return true;
            }
            if (_config.authUser == authUser && (_config.authPass ?? "") == authPass)
            {
                return true;
            }
            return false;
        }

        private void HandshakeReceive()
        {
            try
            {
                int bytesRead = _firstPacketLength;

                if (bytesRead > 1)
                {
                    if ((!(_config.authUser != null && _config.authUser.Length > 0) || Util.Utils.isMatchSubNet(((IPEndPoint)_connection.RemoteEndPoint).Address, "127.0.0.0/8"))
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
                    Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
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
                _remoteHeaderSendBuffer = new byte[2 + addr.Count + 2];
                _remoteHeaderSendBuffer[0] = 3;
                _remoteHeaderSendBuffer[1] = (byte)addr.Count;
                Array.Copy(addr.ToArray(), 0, _remoteHeaderSendBuffer, 2, addr.Count);
                _remoteHeaderSendBuffer[2 + addr.Count] = dataSockSend[2];
                _remoteHeaderSendBuffer[2 + addr.Count + 1] = dataSockSend[3];
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    dataSockSend.Add(_firstPacket[4 + i]);
                }
                _remoteHeaderSendBuffer = new byte[1 + 4 + 2];
                _remoteHeaderSendBuffer[0] = 1;
                Array.Copy(dataSockSend.ToArray(), 4, _remoteHeaderSendBuffer, 1, 4);
                _remoteHeaderSendBuffer[1 + 4] = dataSockSend[2];
                _remoteHeaderSendBuffer[1 + 4 + 1] = dataSockSend[3];
            }
            command = 1; // Set TCP connect command
            _connection.BeginSend(dataSockSend.ToArray(), 0, dataSockSend.Count, 0, new AsyncCallback(StartConnect), null);
        }

        private void RspSocks5HandshakeReceive()
        {
            byte[] response = { 5, 0 };
            if (_firstPacket[0] != 5)
            {
                response = new byte[] { 0, 91 };
                Console.WriteLine("socks 4/5 protocol error");
            }
            if ((_config.authUser != null && _config.authUser.Length > 0) && !Util.Utils.isMatchSubNet(((IPEndPoint)_connection.RemoteEndPoint).Address, "127.0.0.0/8"))
            {
                response[1] = 2;
                _connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(HandshakeAuthSendCallback), null);
            }
            else
            {
                _connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(HandshakeSendCallback), null);
            }
        }

        private void HandshakeAuthSendCallback(IAsyncResult ar)
        {
            try
            {
                _connection.EndSend(ar);

                _connection.BeginReceive(_connetionRecvBuffer, 0, 1024, 0,
                    new AsyncCallback(HandshakeAuthReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void HandshakeAuthReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = _connection.EndReceive(ar);

                if (bytesRead >= 3)
                {
                    byte user_len = _connetionRecvBuffer[1];
                    byte pass_len = _connetionRecvBuffer[user_len + 2];
                    byte[] response = { 1, 0 };
                    string user = Encoding.UTF8.GetString(_connetionRecvBuffer, 2, user_len);
                    string pass = Encoding.UTF8.GetString(_connetionRecvBuffer, user_len + 3, pass_len);
                    if (AuthConnection(_connection, user, pass))
                    {
                        _connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(HandshakeSendCallback), null);
                    }
                }
                else
                {
                    Console.WriteLine("failed to recv data in HandshakeAuthReceiveCallback");
                    Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void HandshakeSendCallback(IAsyncResult ar)
        {
            try
            {
                _connection.EndSend(ar);

                // +----+-----+-------+------+----------+----------+
                // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                _connection.BeginReceive(_connetionRecvBuffer, 0, 1024, 0,
                    new AsyncCallback(HandshakeReceive2Callback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void HandshakeReceive2Callback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = _connection.EndReceive(ar);

                if (bytesRead >= 3)
                {
                    command = _connetionRecvBuffer[1];
                    if (bytesRead > 3)
                    {
                        _remoteHeaderSendBuffer = new byte[bytesRead - 3];
                        Array.Copy(_connetionRecvBuffer, 3, _remoteHeaderSendBuffer, 0, _remoteHeaderSendBuffer.Length);
                    }
                    else
                    {
                        _remoteHeaderSendBuffer = null;
                    }

                    if (command == 3) // UDP
                    {
                        if (bytesRead >= 5)
                        {
                            if (_remoteHeaderSendBuffer == null)
                            {
                                _remoteHeaderSendBuffer = new byte[bytesRead - 3];
                                Array.Copy(_connetionRecvBuffer, 3, _remoteHeaderSendBuffer, 0, _remoteHeaderSendBuffer.Length);
                            }
                            else
                            {
                                Array.Resize(ref _remoteHeaderSendBuffer, _remoteHeaderSendBuffer.Length + bytesRead - 3);
                                Array.Copy(_connetionRecvBuffer, 3, _remoteHeaderSendBuffer, _remoteHeaderSendBuffer.Length - bytesRead, bytesRead);
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
                    Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void RspSocks5UDPHeader(int bytesRead)
        {
            bool ipv6 = _connection.AddressFamily == AddressFamily.InterNetworkV6;
            int udpPort = 0;
            if (bytesRead >= 3 + 6)
            {
                ipv6 = _remoteHeaderSendBuffer[0] == 4;
                if (!ipv6)
                    udpPort = _remoteHeaderSendBuffer[5] * 0x100 + _remoteHeaderSendBuffer[6];
                else
                    udpPort = _remoteHeaderSendBuffer[17] * 0x100 + _remoteHeaderSendBuffer[18];
            }
            if (!ipv6)
            {
                _remoteHeaderSendBuffer = new byte[1 + 4 + 2];
                _remoteHeaderSendBuffer[0] = 0x8 | 1;
                _remoteHeaderSendBuffer[5] = (byte)(udpPort / 0x100);
                _remoteHeaderSendBuffer[6] = (byte)(udpPort % 0x100);
            }
            else
            {
                _remoteHeaderSendBuffer = new byte[1 + 16 + 2];
                _remoteHeaderSendBuffer[0] = 0x8 | 4;
                _remoteHeaderSendBuffer[17] = (byte)(udpPort / 0x100);
                _remoteHeaderSendBuffer[18] = (byte)(udpPort % 0x100);
            }

            int port = 0;
            IPAddress ip = ipv6 ? IPAddress.IPv6Any : IPAddress.Any;
            _connectionUDP = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            for (; port < 65536; ++port)
            {
                try
                {
                    _connectionUDP.Bind(new IPEndPoint(ip, port));
                    break;
                }
                catch (Exception)
                {
                    //
                }
            }
            port = ((IPEndPoint)_connectionUDP.LocalEndPoint).Port;
            if (!ipv6)
            {
                byte[] response = { 5, 0, 0, 1,
                                0, 0, 0, 0,
                                (byte)(port / 0x100), (byte)(port % 0x100) };
                byte[] ip_bytes = ((IPEndPoint)_connection.LocalEndPoint).Address.GetAddressBytes();
                Array.Copy(ip_bytes, 0, response, 4, 4);
                _connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
            }
            else
            {
                byte[] response = { 5, 0, 0, 4,
                                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                (byte)(port / 0x100), (byte)(port % 0x100) };
                byte[] ip_bytes = ((IPEndPoint)_connection.LocalEndPoint).Address.GetAddressBytes();
                Array.Copy(ip_bytes, 0, response, 4, 16);
                _connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
            }
        }

        private void RspSocks5TCPHeader()
        {
            if (_connection.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] response = { 5, 0, 0, 1,
                                0, 0, 0, 0,
                                0, 0 };
                _connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
            }
            else
            {
                byte[] response = { 5, 0, 0, 4,
                                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                0, 0 };
                _connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartConnect), null);
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
            if (Util.Utils.isMatchSubNet(((IPEndPoint)_connection.RemoteEndPoint).Address, "127.0.0.0/8"))
            {
                httpProxyState.httpAuthUser = "";
                httpProxyState.httpAuthPass = "";
            }
            else
            {
                httpProxyState.httpAuthUser = _config.authUser;
                httpProxyState.httpAuthPass = _config.authPass;
            }
            int err = httpProxyState.HandshakeReceive(_firstPacket, _firstPacketLength, ref _remoteHeaderSendBuffer);
            if (err == 1)
            {
                _connection.BeginReceive(_connetionRecvBuffer, 0, _firstPacket.Length, 0,
                    new AsyncCallback(HttpHandshakeRecv), null);
            }
            else if (err == 2)
            {
                string dataSend = httpProxyState.Http407();
                byte[] httpData = System.Text.Encoding.UTF8.GetBytes(dataSend);
                _connection.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(HttpHandshakeAuthEndSend), null);
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
                _connection.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(StartConnect), null);
            }
            else if (err == 500)
            {
                string dataSend = httpProxyState.Http500();
                byte[] httpData = System.Text.Encoding.UTF8.GetBytes(dataSend);
                _connection.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(HttpHandshakeAuthEndSend), null);
            }
        }

        private void HttpHandshakeAuthEndSend(IAsyncResult ar)
        {
            try
            {
                _connection.EndSend(ar);
                _connection.BeginReceive(_connetionRecvBuffer, 0, _firstPacket.Length, 0,
                    new AsyncCallback(HttpHandshakeRecv), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void HttpHandshakeRecv(IAsyncResult ar)
        {
            try
            {
                int bytesRead = _connection.EndReceive(ar);
                if (bytesRead > 0)
                {
                    Array.Copy(_connetionRecvBuffer, _firstPacket, bytesRead);
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
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void StartConnect(IAsyncResult ar)
        {
            try
            {
                _connection.EndSend(ar);
                Connect();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void Connect()
        {
            int local_port = ((IPEndPoint)_connection.LocalEndPoint).Port;
            Handler handler = new Handler();

            handler.getCurrentServer = delegate (string targetURI, bool usingRandom, bool forceRandom) { return _config.GetCurrentServer(targetURI, usingRandom, forceRandom); };
            handler.keepCurrentServer = delegate (string targetURI, string id) { _config.KeepCurrentServer(targetURI, id); };
            handler.connection = _connection;
            handler.connectionUDP = _connectionUDP;
            handler.cfg.reconnectTimesRemain = _config.reconnectTimes;
            handler.cfg.forceRandom = _config.random;
            handler.setServerTransferTotal(_transfer);
            if (_config.proxyEnable)
            {
                handler.cfg.proxyType = _config.proxyType;
                handler.cfg.socks5RemoteHost = _config.proxyHost;
                handler.cfg.socks5RemotePort = _config.proxyPort;
                handler.cfg.socks5RemoteUsername = _config.proxyAuthUser;
                handler.cfg.socks5RemotePassword = _config.proxyAuthPass;
                handler.cfg.proxyUserAgent = _config.proxyUserAgent;
            }
            handler.cfg.TTL = _config.TTL;
            handler.cfg.connect_timeout = _config.connect_timeout;
            handler.cfg.autoSwitchOff = _config.autoBan;
            if (_config.dns_server != null && _config.dns_server.Length > 0)
            {
                handler.cfg.dns_servers = _config.dns_server;
            }
            if (_config.GetPortMapCache().ContainsKey(local_port))
            {
                PortMapConfigCache cfg = _config.GetPortMapCache()[local_port];
                if (cfg.id == cfg.server.id)
                {
                    handler.select_server = cfg.server;
                    byte[] addr = System.Text.Encoding.UTF8.GetBytes(cfg.server_addr);
                    byte[] newFirstPacket = new byte[_firstPacketLength + addr.Length + 4];
                    newFirstPacket[0] = 3;
                    newFirstPacket[1] = (byte)addr.Length;
                    Array.Copy(addr, 0, newFirstPacket, 2, addr.Length);
                    newFirstPacket[addr.Length + 2] = (byte)(cfg.server_port / 256);
                    newFirstPacket[addr.Length + 3] = (byte)(cfg.server_port % 256);
                    Array.Copy(_firstPacket, 0, newFirstPacket, addr.Length + 4, _firstPacketLength);
                    _remoteHeaderSendBuffer = newFirstPacket;
                }
            }
            handler.Start(_remoteHeaderSendBuffer, _remoteHeaderSendBuffer.Length);
        }
    }
}
