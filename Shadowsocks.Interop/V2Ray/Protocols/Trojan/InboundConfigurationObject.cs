using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Protocols.Trojan;

public class InboundConfigurationObject
{
    public List<ClientObject> Clients { get; set; } = new();
    public List<FallbackObject> Fallbacks { get; set; } = new();
}