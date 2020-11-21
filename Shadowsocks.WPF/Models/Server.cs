namespace Shadowsocks.WPF.Models
{
    public class Server : Shadowsocks.Models.Server
    {
        /// <summary>
        /// Gets or sets the amount of data ingress in bytes.
        /// </summary>
        public ulong BytesIngress { get; set; }

        /// <summary>
        /// Gets or sets the amount of data egress in bytes.
        /// </summary>
        public ulong BytesEgress { get; set; }

        public Server()
        {
            BytesIngress = 0UL;
            BytesEgress = 0UL;
        }
    }
}
