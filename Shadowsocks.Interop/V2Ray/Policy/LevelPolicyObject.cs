namespace Shadowsocks.Interop.V2Ray.Policy;

public class LevelPolicyObject
{
    public int? Handshake { get; set; }
    public int? ConnIdle { get; set; }
    public int? UplinkOnly { get; set; }
    public int? DownLinkOnly { get; set; }
    public bool? StatsUserUplink { get; set; }
    public bool? StatsUserDownLink { get; set; }
    public int? BufferSize { get; set; }

    public static LevelPolicyObject Default => new()
    {
        Handshake = 4,
        ConnIdle = 300,
        UplinkOnly = 2,
        DownLinkOnly = 5,
        StatsUserUplink = false,
        StatsUserDownLink = false,
        BufferSize = 512,
    };
}