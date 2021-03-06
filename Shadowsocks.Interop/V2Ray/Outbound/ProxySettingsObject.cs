namespace Shadowsocks.Interop.V2Ray.Outbound
{
    public class ProxySettingsObject
    {
        /// <summary>
        /// Gets or sets the tag of the outbound
        /// used as the proxy.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets whether to keep the protocol
        /// itself's transport layer intact.
        /// Defaults to false, or only proxy internal TCP traffic.
        /// Set to true to proxy the protocol.
        /// The tag will act as a forward proxy.
        /// </summary>
        public bool TransportLayer { get; set; }

        public ProxySettingsObject()
        {
            Tag = "";
        }

        public static ProxySettingsObject Default => new()
        {
            TransportLayer = true,
        };
    }
}
