using Shadowsocks.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.SsRust
{
    public class Config : IGroup<Server>
    {
        /// <inheritdoc/>
        public int Version { get; set; }

        /// <inheritdoc/>
        public List<Server> Servers { get; set; }

        /// <summary>
        /// Gets or sets the listening address.
        /// </summary>
        public string LocalAddress { get; set; }

        /// <summary>
        /// Gets or sets the listening port.
        /// </summary>
        public int LocalPort { get; set; }

        /// <inheritdoc cref="Server.Host"/>
        [JsonPropertyName("server")]
        public string? Host { get; set; }

        /// <inheritdoc cref="Server.Port"/>
        [JsonPropertyName("server_port")]
        public int Port { get; set; }

        /// <inheritdoc cref="Server.Password"/>
        public string? Password { get; set; }

        /// <inheritdoc cref="Server.Method"/>
        public string? Method { get; set; }

        /// <inheritdoc cref="Server.Plugin"/>
        public string? Plugin { get; set; }

        /// <inheritdoc cref="Server.PluginOpts"/>
        public string? PluginOpts { get; set; }

        /// <inheritdoc cref="Server.PluginArgs"/>
        public List<string>? PluginArgs { get; set; }

        /// <summary>
        /// Gets or sets the timeout for UDP associations in seconds.
        /// Defaults to 300 seconds (5 minutes).
        /// </summary>
        public int? UdpTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of UDP associations.
        /// Defaults to 0 (unlimited).
        /// </summary>
        public int UdpMaxAssociations { get; set; }

        /// <summary>
        /// Gets or sets the server manager address.
        /// </summary>
        public string? ManagerAddress { get; set; }

        /// <summary>
        /// Gets or sets the server manager port.
        /// </summary>
        public int ManagerPort { get; set; }

        /// <summary>
        /// Gets or sets the DNS server used to resolve hostnames.
        /// </summary>
        public string? Dns { get; set; }

        /// <summary>
        /// Gets or sets the mode.
        /// Defaults to tcp_only.
        /// Can also be tcp_and_udp or udp_only.
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Gets or sets TCP_NODELAY.
        /// Defaults to false.
        /// </summary>
        public bool NoDelay { get; set; }

        /// <summary>
        /// Gets or sets the soft and hard limit of file descriptors.
        /// </summary>
        public int Nofile { get; set; }

        /// <summary>
        /// Gets or sets whether IPv6 addresses take precedence over IPv4 addresses for resolved hostnames.
        /// Defaults to false.
        /// </summary>
        public bool Ipv6First { get; set; }
        
        public Config()
        {
            Version = 1;
            Servers = new();
            LocalAddress = "";
            LocalPort = 1080;
            Mode = "tcp_only";
        }

        /// <summary>
        /// Gets the default configuration for Linux.
        /// </summary>
        public static Config DefaultLinux => new()
        {
            LocalAddress = "::1",
            Mode = "tcp_and_udp",
            NoDelay = true,
            Nofile = 32768,
            Ipv6First = true,
        };

        /// <summary>
        /// Gets the default configuration for Windows.
        /// </summary>
        public static Config DefaultWindows => new()
        {
            LocalAddress = "::1",
            Mode = "tcp_and_udp",
            NoDelay = true,
            Ipv6First = true,
        };
    }
}
