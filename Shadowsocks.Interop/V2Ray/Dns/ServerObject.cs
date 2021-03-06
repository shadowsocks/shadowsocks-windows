using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Dns
{
    public class ServerObject
    {
        /// <summary>
        /// Gets or sets the DNS server address.
        /// Supports UDP and DoH.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the DNS server port.
        /// Defaults to 53.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the client IP
        /// to include in DNS queries.
        /// </summary>
        public string? ClientIp { get; set; }

        /// <summary>
        /// Gets or sets the list of domains
        /// that prefers this DNS server.
        /// </summary>
        public List<string> Domains { get; set; }

        /// <summary>
        /// Gets or sets the ranges of IP addresses
        /// this DNS server is expected to return.
        /// </summary>
        public List<string> ExpectIPs { get; set; }

        public ServerObject()
        {
            Address = "";
            Port = 53;
            Domains = new();
            ExpectIPs = new();
        }
    }
}
