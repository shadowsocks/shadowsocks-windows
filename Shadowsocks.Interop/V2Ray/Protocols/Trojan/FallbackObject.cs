namespace Shadowsocks.Interop.V2Ray.Protocols.Trojan;

public class FallbackObject
{
    public string? Alpn { get; set; }
    public string? Path { get; set; }
    public object Dest { get; set; } = 0;
    public int? Xver { get; set; }

    public static FallbackObject Default => new()
    {
        Alpn = "",
        Path = "",
        Xver = 0,
    };
}