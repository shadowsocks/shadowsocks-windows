using Shadowsocks.Interop.V2Ray.Transport.Header;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class QuicObject
    {
        /// <summary>
        /// Gets or sets the encryption method.
        /// Defaults to "none" (no encryption).
        /// Available values: "none" | "aes-128-gcm" | "chacha20-poly1305"
        /// </summary>
        public string Security { get; set; }

        /// <summary>
        /// Gets or sets the encryption key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the header options.
        /// </summary>
        public HeaderObject Header { get; set; }

        public QuicObject()
        {
            Security = "none";
            Key = "";
            Header = new();
        }
    }
}
