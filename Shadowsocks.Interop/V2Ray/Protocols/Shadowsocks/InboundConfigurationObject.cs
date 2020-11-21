using System;
using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.V2Ray.Protocols.Shadowsocks
{
    public class InboundConfigurationObject
    {
        public string Email { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Method { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Password { get; set; }

        public int Level { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Network { get; set; }

        public InboundConfigurationObject()
        {
            Email = "";
            Method = "chacha20-ietf-poly1305";
            Password = new Guid().ToString();
            Level = 0;
            Network = "tcp,udp";
        }
    }
}
