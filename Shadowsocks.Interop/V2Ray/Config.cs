using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray
{
    public class Config
    {
        public LogObject? Log { get; set; }
        public ApiObject? Api { get; set; }
        public DnsObject? Dns { get; set; }
        public RoutingObject? Routing { get; set; }
        public PolicyObject? Policy { get; set; }
        public List<InboundObject>? Inbounds { get; set; }
        public List<OutboundObject>? Outbounds { get; set; }
        public TransportObject? Transport { get; set; }
        public StatsObject? Stats { get; set; }
        public ReverseObject? Reverse { get; set; }

        /// <summary>
        /// Gets the default configuration.
        /// </summary>
        public static Config Default => new()
        {
            Log = new(),
            Api = ApiObject.Default,
            Dns = new(),
            Routing = new(),
            Policy = PolicyObject.Default,
            Inbounds = new(),
            Outbounds = new(),
            Stats = new(),
        };
    }
}
