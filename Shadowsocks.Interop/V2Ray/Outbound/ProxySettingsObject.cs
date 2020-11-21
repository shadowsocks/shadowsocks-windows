namespace Shadowsocks.Interop.V2Ray.Outbound
{
    public class ProxySettingsObject
    {
        /// <summary>
        /// Gets or sets the tag of the outbound
        /// used as the proxy.
        /// </summary>
        public string Tag { get; set; }

        public ProxySettingsObject()
        {
            Tag = "";
        }
    }
}
