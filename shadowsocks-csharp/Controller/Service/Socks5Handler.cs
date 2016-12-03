using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Model;
using Shadowsocks.Util.Sockets;

namespace Shadowsocks.Controller.Service
{

    class Socks5HandlerFactory : ITCPHandlerFactory
    {
        public bool CanHandle(byte[] firstPacket, int length)
        {
            return length >= 2 && firstPacket[0] == 5;
        }

        public TCPHandler NewHandler(ShadowsocksController controller, Configuration config, TCPRelay tcprelay, Socket socket)
        {
            return new Socks5Handler(controller, config, tcprelay, socket);
        }
    }

    class Socks5Handler : TCPHandler
    {
        private byte _command;
        private int _firstPacketLength;

        public Socks5Handler(ShadowsocksController controller, Configuration config, TCPRelay tcprelay, Socket socket) : base(controller, config, tcprelay, socket, false)
        {
        }

        public override void StartHandshake(byte[] firstPacket, int length)
        {
            if (Closed) return;
            try
            {
                int bytesRead = length;
                if (bytesRead > 1)
                {
                    byte[] response = { 5, 0 };
                    if (firstPacket[0] != 5)
                    {
                        // reject socks 4
                        response = new byte[] { 0, 91 };
                        Logging.Error("socks 5 protocol error");
                    }
                    Connection.BeginSend(response, 0, response.Length, SocketFlags.None, HandshakeSendCallback, null);
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
            if (Closed) return;
            try
            {
                Connection.EndSend(ar);

                // +-----+-----+-------+------+----------+----------+
                // | VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +-----+-----+-------+------+----------+----------+
                // |  1  |  1  | X'00' |  1   | Variable |    2     |
                // +-----+-----+-------+------+----------+----------+
                // Skip first 3 bytes, and read 2 more bytes to analysis the address.
                // 2 more bytes is designed if address is domain then we don't need to read once more to get the addr length.
                // TODO validate
                Connection.BeginReceive(ConnetionRecvBuffer, 0, 3 + 2, SocketFlags.None,
                    handshakeReceive2Callback, null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        private void handshakeReceive2Callback(IAsyncResult ar)
        {
            if (Closed) return;
            try
            {
                int bytesRead = Connection.EndReceive(ar);
                if (bytesRead >= 5)
                {
                    _command = ConnetionRecvBuffer[1];
                    if (_command != 1 && _command != 3)
                    {
                        Logging.Debug("Unsupported CMD=" + _command);
                        Close();
                    }
                    else
                    {
                        int atyp = ConnetionRecvBuffer[3];

                        switch (atyp)
                        {
                            case 1: // IPv4 address, 4 bytes
                                ReadAddress(4 + 2 - 1);
                                break;
                            case 3: // domain name, length + str
                                int len = ConnetionRecvBuffer[4];
                                ReadAddress(len + 2);
                                break;
                            case 4: // IPv6 address, 16 bytes
                                ReadAddress(16 + 2 - 1);
                                break;
                            default:
                                Logging.Debug("Unsupported ATYP=" + atyp);
                                Close();
                                break;
                        }
                    }
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


        private void ReadAddress(int bytesRemain)
        {
            Array.Copy(ConnetionRecvBuffer, 3, ConnetionRecvBuffer, 0, 2);

            // Read the remain address bytes
            Connection.BeginReceive(ConnetionRecvBuffer, 2, RecvSize - 2, SocketFlags.None, OnAddressFullyRead, bytesRemain);
        }

        private void OnAddressFullyRead(IAsyncResult ar)
        {
            if (Closed) return;
            try
            {
                int bytesRead = Connection.EndReceive(ar);
                int bytesRemain = (int)ar.AsyncState;
                if (bytesRead >= bytesRemain)
                {
                    _firstPacketLength = bytesRead + 2;

                    int atyp = ConnetionRecvBuffer[0];

                    string dst_addr = "Unknown";
                    int dst_port = -1;
                    switch (atyp)
                    {
                        case 1: // IPv4 address, 4 bytes
                            dst_addr = new IPAddress(ConnetionRecvBuffer.Skip(1).Take(4).ToArray()).ToString();
                            dst_port = (ConnetionRecvBuffer[5] << 8) + ConnetionRecvBuffer[6];

                            break;
                        case 3: // domain name, length + str
                            int len = ConnetionRecvBuffer[1];
                            dst_addr = System.Text.Encoding.UTF8.GetString(ConnetionRecvBuffer, 2, len);
                            dst_port = (ConnetionRecvBuffer[len + 2] << 8) + ConnetionRecvBuffer[len + 3];

                            break;
                        case 4: // IPv6 address, 16 bytes
                            dst_addr = $"[{new IPAddress(ConnetionRecvBuffer.Skip(1).Take(16).ToArray())}]";
                            dst_port = (ConnetionRecvBuffer[17] << 8) + ConnetionRecvBuffer[18];

                            break;
                    }
                    if (Config.isVerboseLogging)
                    {
                        Logging.Info($"connect to {dst_addr}:{dst_port}");
                    }

                    var destEndPoint = SocketUtil.GetEndPoint(dst_addr, dst_port);

                    if (_command == 1)
                    {
                        byte[] response = { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
                        Connection.BeginSend(response, 0, response.Length, SocketFlags.None,
                            ResponseCallback, destEndPoint);
                    }
                    else if (_command == 3)
                    {
                        HandleUDPAssociate();
                    }
                }
                else
                {
                    Logging.Debug("failed to recv data in Shadowsocks.Controller.TCPHandler.OnAddressFullyRead()");
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
            IPEndPoint endPoint = (IPEndPoint)Connection.LocalEndPoint;
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
            Connection.BeginSend(response, 0, response.Length, SocketFlags.None, new AsyncCallback(ReadAll), true);
        }

        private void ReadAll(IAsyncResult ar)
        {
            if (Closed) return;
            try
            {
                if (ar.AsyncState != null)
                {
                    Connection.EndSend(ar);
                    Connection.BeginReceive(ConnetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(ReadAll), null);
                }
                else
                {
                    int bytesRead = Connection.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        Connection.BeginReceive(ConnetionRecvBuffer, 0, RecvSize, SocketFlags.None, new AsyncCallback(ReadAll), null);
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
                Connection.EndSend(ar);
                StartConnect((EndPoint) ar.AsyncState);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }

        protected override void OnServerConnected(AsyncSession session)
        {
            BeginSendToServer(_firstPacketLength, session, FirstPackageSendCallback);
        }

        private void FirstPackageSendCallback(IAsyncResult ar)
        {
            if (Closed) return;
            try
            {
                var session = EndSendToServer(ar);
                StartPipe(session);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Close();
            }
        }
    }
}
