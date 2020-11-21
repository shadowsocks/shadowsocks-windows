namespace Shadowsocks.Interop.V2Ray.Policy
{
    public class SystemPolicyObject
    {
        public bool StatsInboundUplink { get; set; }
        public bool StatsInboundDownlink { get; set; }
        public bool StatsOutboundUplink { get; set; }
        public bool StatsOutboundDownlink { get; set; }

        public static SystemPolicyObject Default => new()
        {
            StatsInboundUplink = true,
            StatsInboundDownlink = true,
            StatsOutboundUplink = true,
            StatsOutboundDownlink = true,
        };
    }
}
