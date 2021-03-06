using System;
using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.V2Ray.Protocols.Shadowsocks
{
    public class InboundConfigurationObject
    {
        public string? Email { get; set; }
        
        public string Method { get; set; }

        public string Password { get; set; }

        public int? Level { get; set; }

        public string Network { get; set; }

        public InboundConfigurationObject()
        {
            Method = "chacha20-ietf-poly1305";
            Password = Guid.NewGuid().ToString();
            Network = "tcp,udp";
        }
    }
}
