namespace Shadowsocks.Interop.V2Ray.Protocols.Freedom
{
    public class OutboundConfigurationObject
    {
        public string DomainStrategy { get; set; }
        public string? Redirect { get; set; }
        public int? UserLevel { get; set; }

        public OutboundConfigurationObject()
        {
            DomainStrategy = "AsIs";
        }
    }
}
