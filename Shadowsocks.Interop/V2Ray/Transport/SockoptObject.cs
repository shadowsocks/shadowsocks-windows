namespace Shadowsocks.Interop.V2Ray.Transport;

public class SockOptObject
{
    public int Mark { get; set; }
    public bool TcpFastOpen { get; set; }
    public string? Tproxy { get; set; }

    public static SockOptObject DefaultLinux => new()
    {
        Tproxy = "off",
    };
}