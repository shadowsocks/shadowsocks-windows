namespace Shadowsocks.Interop.V2Ray.Transport.Header
{
    public class HeaderObject
    {
        /// <summary>
        /// Gets or sets the header type.
        /// Defaults to none.
        /// Available values:
        /// none
        /// srtp
        /// utp
        /// wechat-video
        /// dtls
        /// wireguard
        /// </summary>
        public string Type { get; set; }

        public HeaderObject()
        {
            Type = "none";
        }
    }
}
