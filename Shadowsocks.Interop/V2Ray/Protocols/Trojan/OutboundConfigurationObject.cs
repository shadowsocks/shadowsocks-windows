using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Protocols.Trojan
{
    public class OutboundConfigurationObject
    {
        public List<ServerObject> Servers { get; set; }

        public OutboundConfigurationObject()
        {
            Servers = new();
        }

        public OutboundConfigurationObject(string address, int port, string password)
        {
            Servers = new()
            {
                new(address, port, password),
            };
        }
    }
}
