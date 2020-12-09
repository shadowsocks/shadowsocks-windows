using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Protocols.Socks
{
    public class InboundConfigurationObject
    {
        public string? Auth { get; set; }
        public List<AccountObject>? Accounts { get; set; }
        public bool? Udp { get; set; }
        public string? Ip { get; set; }
        public int? UserLevel { get; set; }

        public static InboundConfigurationObject Default => new()
        {
            Udp = true,
            Ip = "127.0.0.1",
        };
    }
}
