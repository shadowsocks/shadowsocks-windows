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

        public SniffingObject()
        {
            Enabled = true;
            DestOverride = new()
            {
                "http",
                "tls",
            };
        }
    }
}
