using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.V2Ray.Routing
{
    public class RuleObject
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Type { get; set; }
        public List<string>? Domain { get; set; }
        public List<string>? Ip { get; set; }
        public object? Port { get; set; }
        public object? SourcePort { get; set; }
        public string? Network { get; set; }
        public List<string>? Source { get; set; }
        public List<string>? User { get; set; }
        public List<string>? InboundTag { get; set; }
        public List<string>? Protocol { get; set; }
        public string? Attrs { get; set; }
        public string? OutboundTag { get; set; }
        public string? BalancerTag { get; set; }

        public RuleObject()
        {
            Type = "field";
        }

        public static RuleObject DefaultOutbound => new()
        {
            OutboundTag = "",
        };

        public static RuleObject DefaultBalancer => new()
        {
            BalancerTag = "",
        };
    }
}
