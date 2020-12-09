namespace Shadowsocks.Interop.V2Ray.Reverse
{
    public class BridgeObject
    {
        /// <summary>
        /// Gets or sets the inbound tag for the bridge.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets the domain name for the bridge.
        /// Can be omitted.
        /// </summary>
        public string? Domain { get; set; }

        public BridgeObject()
        {
            Tag = "";
        }
    }
}
