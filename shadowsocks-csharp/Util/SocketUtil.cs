using System;
using System.Net;
using System.Net.Sockets;

namespace Shadowsocks.Util
{
    public static class SocketUtil
    {
        private class DnsEndPoint2 : DnsEndPoint
        {
            public DnsEndPoint2(string host, int port) : base(host, port)
            {
            }

            public DnsEndPoint2(string host, int port, AddressFamily addressFamily) : base(host, port, addressFamily)
            {
            }

            public override string ToString()
            {
                return this.Host + ":" + this.Port;
            }
        }

        public static EndPoint GetEndPoint(string host, int port)
        {
            IPAddress ipAddress;
            bool parsed = IPAddress.TryParse(host, out ipAddress);
            if (parsed)
            {
                return new IPEndPoint(ipAddress, port);
            }

            // maybe is a domain name
            return new DnsEndPoint2(host, port);
        }

        public static Socket CreateSocket(EndPoint endPoint, ProtocolType protocolType = ProtocolType.Tcp)
        {
            SocketType socketType;
            switch (protocolType)
            {
                case ProtocolType.Tcp:
                    socketType = SocketType.Stream;
                    break;
                case ProtocolType.Udp:
                    socketType = SocketType.Dgram;
                    break;
                default:
                    throw new NotSupportedException("Protocol " + protocolType + " doesn't supported!");
            }

            if (endPoint is DnsEndPoint)
            {
                // use dual-mode socket
                var socket = new Socket(AddressFamily.InterNetworkV6, socketType, protocolType);
                socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);

                return socket;
            }
            else
            {
                return new Socket(endPoint.AddressFamily, socketType, protocolType);
            }
        }
    }
}
