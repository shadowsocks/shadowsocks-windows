using Shadowsocks.Interop.V2Ray.Transport.Header;

namespace Shadowsocks.Interop.V2Ray.Transport;

public class KcpObject
{
    public int Mtu { get; set; } = 1350;
    public int Tti { get; set; } = 50;
    public int UplinkCapacity { get; set; } = 5;
    public int DownLinkCapacity { get; set; } = 20;
    public bool Congestion { get; set; } = false;
    public int ReadBufferSize { get; set; } = 2;
    public int WriteBufferSize { get; set; } = 2;
    public HeaderObject Header { get; set; } = new();
    public string Seed { get; set; } = "";
}