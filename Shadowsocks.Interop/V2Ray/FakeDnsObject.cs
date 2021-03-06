namespace Shadowsocks.Interop.V2Ray
{
    public class FakeDnsObject
    {
        /// <summary>
        /// Gets or sets the IP pool CIDR.
        /// </summary>
        public string IpPool { get; set; } = "240.0.0.0/8";

        /// <summary>
        /// Gets or sets the IP pool size.
        /// </summary>
        public long PoolSize { get; set; } = 65535L;
    }
}
