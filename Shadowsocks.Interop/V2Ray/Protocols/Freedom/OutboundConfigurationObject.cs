namespace Shadowsocks.Interop.V2Ray.Protocols.Freedom;

public class OutboundConfigurationObject
{
    public string DomainStrategy { get; set; } = "AsIs";
    public string? Redirect { get; set; }
    public int? UserLevel { get; set; }
}