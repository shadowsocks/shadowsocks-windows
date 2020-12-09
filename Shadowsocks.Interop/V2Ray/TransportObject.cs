using Shadowsocks.Interop.V2Ray.Transport;

namespace Shadowsocks.Interop.V2Ray
{
    public class TransportObject
    {
        public TcpObject? TcpSettings { get; set; }
        public KcpObject? KcpSettings { get; set; }
        public WebSocketObject? WsSettings { get; set; }
        public HttpObject? HttpSettings { get; set; }
        public QuicObject? QuicSettings { get; set; }
        public DomainSocketObject? DsSettings { get; set; }
    }
}
