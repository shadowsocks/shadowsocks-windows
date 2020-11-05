using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Net.Proxy
{
    public class Socks5Proxy : IProxy
    {
        private readonly Socket _remote = new Socket(SocketType.Stream, ProtocolType.Tcp);

        private const int Socks5PktMaxSize = 4 + 16 + 2;
        private readonly byte[] _receiveBuffer = new byte[Socks5PktMaxSize];

        public EndPoint LocalEndPoint => _remote.LocalEndPoint;
        public EndPoint ProxyEndPoint { get; private set; }
        public EndPoint DestEndPoint { get; private set; }

        public void Shutdown(SocketShutdown how)
        {
            _remote.Shutdown(how);
        }

        public void Close()
        {
            _remote.Dispose();
        }

        public async Task ConnectProxyAsync(EndPoint remoteEP, NetworkCredential auth = null, CancellationToken token = default)
        {
            ProxyEndPoint = remoteEP;
            await _remote.ConnectAsync(remoteEP);
            await _remote.SendAsync(new byte[] { 5, 1, 0 }, SocketFlags.None);
            if (await _remote.ReceiveAsync(_receiveBuffer.AsMemory(0, 2), SocketFlags.None) != 2)
            {
                throw new Exception("Proxy handshake failed");
            }
            if (_receiveBuffer[0] != 5 || _receiveBuffer[1] != 0)
            {
                throw new Exception("Proxy handshake failed");
            }
        }

        public async Task ConnectRemoteAsync(EndPoint destEndPoint, CancellationToken token = default)
        {
            // TODO: support SOCKS5 auth
            DestEndPoint = destEndPoint;

            byte[] request;
            byte atyp;
            int port;
            if (destEndPoint is DnsEndPoint dep)
            {
                // is a domain name, we will leave it to server

                atyp = 3; // DOMAINNAME
                var enc = Encoding.UTF8;
                var hostByteCount = enc.GetByteCount(dep.Host);

                request = new byte[4 + 1/*length byte*/ + hostByteCount + 2];
                request[4] = (byte)hostByteCount;
                enc.GetBytes(dep.Host, 0, dep.Host.Length, request, 5);

                port = dep.Port;
            }
            else
            {
                switch (DestEndPoint.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        request = new byte[4 + 4 + 2];
                        atyp = 1; // IP V4 address
                        break;
                    case AddressFamily.InterNetworkV6:
                        request = new byte[4 + 16 + 2];
                        atyp = 4; // IP V6 address
                        break;
                    default:
                        throw new Exception("Proxy request failed");
                }
                port = ((IPEndPoint)DestEndPoint).Port;
                var addr = ((IPEndPoint)DestEndPoint).Address.GetAddressBytes();
                Array.Copy(addr, 0, request, 4, request.Length - 4 - 2);
            }

            request[0] = 5;
            request[1] = 1;
            request[2] = 0;
            request[3] = atyp;
            request[^2] = (byte)((port >> 8) & 0xff);
            request[^1] = (byte)(port & 0xff);

            await _remote.SendAsync(request, SocketFlags.None, token);

            if (await _remote.ReceiveAsync(_receiveBuffer.AsMemory(0, 4), SocketFlags.None, token) != 4)
            {
                throw new Exception("Proxy request failed");
            };
            if (_receiveBuffer[0] != 5 || _receiveBuffer[1] != 0)
            {
                throw new Exception("Proxy request failed");
            }
            var addrLen = _receiveBuffer[3] switch
            {
                1 => 6,
                4 => 18,
                _ => throw new NotImplementedException(),
            };
            if (await _remote.ReceiveAsync(_receiveBuffer.AsMemory(0, addrLen), SocketFlags.None, token) != addrLen)
            {
                throw new Exception("Proxy request failed");
            }
        }

        public async Task<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            return await _remote.SendAsync(buffer, SocketFlags.None, token);
        }

        public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            return await _remote.ReceiveAsync(buffer, SocketFlags.None, token);
        }
    }
}
