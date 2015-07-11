using System;
using System.Collections.Generic;
using System.Text;
using Shadowsocks.Encryption;
using Shadowsocks.Model;
using System.Net.Sockets;
using System.Net;

namespace Shadowsocks.Controller
{
    class UDPRelay : Listener.Service
    {
        private Configuration _config;
        public UDPRelay(Configuration config)
        {
            this._config = config;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Udp)
            {
                return false;
            }
            if (length < 4)
            {
                return false;
            }
            Listener.UDPState udpState = (Listener.UDPState)state;
            // TODO add cache
            UDPHandler handler = new UDPHandler(socket, _config.GetCurrentServer(), (IPEndPoint)udpState.remoteEndPoint);
            handler.Send(firstPacket, length);
            handler.Receive();
            return true;
        }

        class UDPHandler
        {
            private Socket _local;
            private Socket _remote;

            private Server _server;
            private byte[] _buffer = new byte[1500];

            private IPEndPoint _localEndPoint;
            private IPEndPoint _remoteEndPoint;

            public UDPHandler(Socket local, Server server, IPEndPoint localEndPoint)
            {
                // TODO add timeout
                _local = local;
                _server = server;
                _localEndPoint = localEndPoint;

                // TODO async resolving
                IPAddress ipAddress;
                bool parsed = IPAddress.TryParse(server.server, out ipAddress);
                if (!parsed)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(server.server);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                _remoteEndPoint = new IPEndPoint(ipAddress, server.server_port);
                _remote = new Socket(_remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            }
            public void Send(byte[] data, int length)
            {
                IEncryptor encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                byte[] dataIn = new byte[length - 3];
                Array.Copy(data, 3, dataIn, 0, length - 3);
                byte[] dataOut = new byte[length - 3 + 16];
                int outlen;
                encryptor.Encrypt(dataIn, dataIn.Length, dataOut, out outlen);
                _remote.SendTo(dataOut, _remoteEndPoint);
            }
            public void Receive()
            {
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                _remote.BeginReceiveFrom(_buffer, 0, _buffer.Length, 0, ref remoteEndPoint, new AsyncCallback(RecvFromCallback), null);
            }
            public void RecvFromCallback(IAsyncResult ar)
            {
                try
                {
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int bytesRead = _remote.EndReceiveFrom(ar, ref remoteEndPoint);

                    byte[] dataOut = new byte[bytesRead];
                    int outlen;

                    IEncryptor encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                    encryptor.Decrypt(_buffer, bytesRead, dataOut, out outlen);

                    byte[] sendBuf = new byte[outlen + 3];
                    Array.Copy(dataOut, 0, sendBuf, 3, outlen);

                    _local.SendTo(sendBuf, outlen + 3, 0, _localEndPoint);
                    Receive();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception e)
                {
                }
                finally
                {
                }
            }
        }
    }
}
