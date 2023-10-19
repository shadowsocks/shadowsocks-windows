using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Inbound;

public class SniffingObject
{
    /// <summary>
    /// Gets or sets whether to enable sniffing.
    /// Defaults to true (enabled).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of protocols that destination override is enabled.
    /// </summary>
    public List<string> DestOverride { get; set; } =
    [
        "http",
        "tls",
    ];

    /// <summary>
    /// Gets or sets whether the target address is sniffed
    /// solely based on metadata.
    /// Defaults to false.
    /// Change to true to use FakeDNS.
    /// </summary>
    public bool MetadataOnly { get; set; }

    public static SniffingObject Default => new()
    {
        Enabled = false,
        DestOverride =
        [
            "http",
            "tls",
        ],
    };

    public static SniffingObject DefaultFakeDns => new()
    {
        Enabled = true,
        DestOverride =
        [
            "http",
            "tls",
            "fakedns",
        ],
        MetadataOnly = true,
    };
}