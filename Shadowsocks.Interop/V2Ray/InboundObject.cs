using Shadowsocks.Interop.V2Ray.Inbound;
using Shadowsocks.Interop.V2Ray.Transport;

namespace Shadowsocks.Interop.V2Ray
{
    public class InboundObject
    {
        public string Tag { get; set; }
        public string? Listen { get; set; }
        public object? Port { get; set; }
        public string Protocol { get; set; }
        public object? Settings { get; set; }
        public StreamSettingsObject? StreamSettings { get; set; }
        public SniffingObject? Sniffing { get; set; }
        public AllocateObject? Allocate { get; set; }

        public InboundObject()
        {
            Tag = "";
            Protocol = "";
        }

        public static InboundObject DefaultLocalSocks => new()
        {
            Tag = "socks-in",
            Listen = "127.0.0.1",
            Port = 1080,
            Protocol = "socks",
            Settings = Protocols.Socks.InboundConfigurationObject.Default,
            Sniffing = SniffingObject.Default,
        };

        public static InboundObject DefaultLocalHttp => new()
        {
            Tag = "http-in",
            Listen = "127.0.0.1",
            Port = 8080,
            Protocol = "http",
            Sniffing = SniffingObject.Default,
        };
    }
}
