using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.V2Ray
{
    public class Config
    {
        public LogObject Log { get; set; }
        public ApiObject Api { get; set; }
        public DnsObject Dns { get; set; }
        public RoutingObject Routing { get; set; }
        public PolicyObject Policy { get; set; }
        public InboundObject Inbounds { get; set; }
        public OutboundObject Outbounds { get; set; }
        public TransportObject Transport { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public StatsObject? Stats { get; set; }
        public ReverseObject Reverse { get; set; }
        
        public Config(bool stats = true)
        {
            Log = new();
            Api = new();
            Dns = new();
            Routing = new();
            Policy = new();
            Inbounds = new();
            Outbounds = new();
            Transport = new();
            Stats = stats ? new() : null;
            Reverse = new();
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
            Transport = new(),
            Stats = new(),
            Reverse = new(),
        };
    }
}
