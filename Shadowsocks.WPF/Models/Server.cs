namespace Shadowsocks.WPF.Models;

public class Server : Shadowsocks.Models.Server
{
    /// <summary>
    /// Gets or sets the amount of data ingress in bytes.
    /// </summary>
    public ulong BytesIngress { get; set; } = 0UL;

    /// <summary>
    /// Gets or sets the amount of data egress in bytes.
    /// </summary>
    public ulong BytesEgress { get; set; } = 0UL;
}