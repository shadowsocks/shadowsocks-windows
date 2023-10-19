using System;

namespace Shadowsocks.Interop.V2Ray.Protocols.Shadowsocks;

public class InboundConfigurationObject
{
    public string? Email { get; set; }

    public string Method { get; set; } = "chacha20-ietf-poly1305";

    public string Password { get; set; } = Guid.NewGuid().ToString();

    public int? Level { get; set; }

    public string Network { get; set; } = "tcp,udp";
}