using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.V2Ray
{
    public class Config
    {
        public LogObject? Log { get; set; }
        public ApiObject? Api { get; set; }
        public DnsObject? Dns { get; set; }
        public RoutingObject Routing { get; set; }
        public PolicyObject? Policy { get; set; }
        public InboundObject Inbounds { get; set; }
        public OutboundObject Outbounds { get; set; }
        public TransportObject? Transport { get; set; }
        public StatsObject? Stats { get; set; }
        public ReverseObject? Reverse { get; set; }
        
        public Config()
        {
            Routing = new();
            Inbounds = new();
            Outbounds = new();
        }

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
