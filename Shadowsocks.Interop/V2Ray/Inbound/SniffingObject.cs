using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Inbound
{
    public class SniffingObject
    {
        /// <summary>
        /// Gets or sets whether to enable sniffing.
        /// Defaults to true (enabled).
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the list of protocols that destination override is enabled.
        /// </summary>
        public List<string> DestOverride { get; set; }

        /// <summary>
        /// Gets or sets whether the target address is sniffed
        /// solely based on metadata.
        /// Defaults to false.
        /// Change to true to use FakeDNS.
        /// </summary>
        public bool MetadataOnly { get; set; }

        public SniffingObject()
        {
            Enabled = true;
            DestOverride = new()
            {
                "http",
                "tls",
            };
        }

        public static SniffingObject Default => new()
        {
            Enabled = false,
            DestOverride = new()
            {
                "http",
                "tls",
            },
        };

        public static SniffingObject DefaultFakeDns => new()
        {
            Enabled = true,
            DestOverride = new()
            {
                "http",
                "tls",
                "fakedns",
            },
            MetadataOnly = true,
        };
    }
}
