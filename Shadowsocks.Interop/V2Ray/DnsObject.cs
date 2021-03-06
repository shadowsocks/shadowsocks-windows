using Shadowsocks.Interop.V2Ray.Dns;
using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray
{
    public class DnsObject
    {
        /// <summary>
        /// Gets or sets the dictionary storing hosts.
        /// The key is the hostname.
        /// The value can either be a hostname or an IP address.
        /// </summary>
        public Dictionary<string, string> Hosts { get; set; }

        /// <summary>
        /// Gets or sets the list of DNS servers.
        /// A DNS server can either be a <see cref="ServerObject"/> or a string.
        /// </summary>
        public List<object> Servers { get; set; }

        /// <summary>
        /// Gets or sets the client IP used when sending requests to DNS server.
        /// </summary>
        public string? ClientIp { get; set; }

        /// <summary>
        /// Gets or sets whether to disable internal DNS cache.
        /// Defaults to false, or DNS cache is enabled.
        /// </summary>
        public bool DisableCache { get; set; }

        /// <summary>
        /// Gets or sets the inbound tag for DNS traffic.
        /// </summary>
        public string? Tag { get; set; }

        public DnsObject()
        {
            Hosts = new();
            Servers = new();
        }
    }
}
