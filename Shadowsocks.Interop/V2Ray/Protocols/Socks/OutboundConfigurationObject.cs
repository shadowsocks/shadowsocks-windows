using System.Collections.Generic;
using System.Net;

namespace Shadowsocks.Interop.V2Ray.Protocols.Socks
{
    public class OutboundConfigurationObject
    {
        public List<ServerObject> Servers { get; set; }

        public OutboundConfigurationObject()
        {
            Servers = new();
        }

        public OutboundConfigurationObject(DnsEndPoint socksEndPoint, string username = "", string password = "")
        {
            Servers = new()
            {
                new(socksEndPoint, username, password),
            };
        }
    }
}
