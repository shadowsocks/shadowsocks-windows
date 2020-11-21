namespace Shadowsocks.Interop.V2Ray.Reverse
{
    public class PortalObject
    {
        /// <summary>
        /// Gets or sets the outbound tag for the portal.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets the domain name for the portal.
        /// </summary>
        public string Domain { get; set; }

        public PortalObject()
        {
            Tag = "";
            Domain = "";
        }
    }
}
