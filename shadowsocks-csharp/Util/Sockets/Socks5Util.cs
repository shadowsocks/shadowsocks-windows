using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Controller;

namespace Shadowsocks.Util.Sockets
{
    public static class Socks5Util
    {
        public static int HeaderAddrLength(EndPoint addrEp)
        {
            var dep = addrEp as DnsEndPoint;
            if (dep != null)
            {
                var enc = Encoding.UTF8;
                var hostByteCount = enc.GetByteCount(dep.Host);

                return 1 + 1 /*length byte*/+ hostByteCount + 2;
            }

            switch (addrEp.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return 1 + 4 + 2;
                case AddressFamily.InterNetworkV6:
                    return 1 + 16 + 2;
                default:
                    throw new Exception(I18N.GetString("Proxy request failed"));
            }
        }

        public static int FillHeaderAddr(byte[] buffer, int offset, EndPoint addrEp)
        {
            byte atyp;
            int port;
            int len;

            var dep = addrEp as DnsEndPoint;
            if (dep != null)
            {
                // is a domain name, we will leave it to server

                atyp = 3; // DOMAINNAME
                var enc = Encoding.UTF8;
                var hostByteCount = enc.GetByteCount(dep.Host);

                len = 1 + 1 /*length byte*/+ hostByteCount + 2;

                buffer[offset + 1] = (byte)hostByteCount;
                enc.GetBytes(dep.Host, 0, dep.Host.Length, buffer, offset + 2);

                port = dep.Port;
            }
            else
            {
                switch (addrEp.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        len = 1 + 4 + 2;
                        atyp = 1; // IP V4 address
                        break;
                    case AddressFamily.InterNetworkV6:
                        len = 1 + 16 + 2;
                        atyp = 4; // IP V6 address
                        break;
                    default:
                        throw new Exception(I18N.GetString("Proxy request failed"));
                }
                port = ((IPEndPoint)addrEp).Port;
                var addr = ((IPEndPoint)addrEp).Address.GetAddressBytes();
                Array.Copy(addr, 0, buffer, offset + 1, len - 1 - 2);
            }

            buffer[offset] = atyp;
            buffer[offset + len - 2] = (byte)((port >> 8) & 0xff);
            buffer[offset + len - 1] = (byte)(port & 0xff);

            return len;
        }
    }
}
