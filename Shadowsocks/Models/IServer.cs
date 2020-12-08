using System;
using System.Text.Json.Serialization;

namespace Shadowsocks.Models
{
    public interface IServer : IEquatable<IServer>
    {
        /// <summary>
        /// Gets or sets the server address.
        /// </summary>
        [JsonPropertyName("server")]
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the server port.
        /// </summary>
        [JsonPropertyName("server_port")]
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the password for the server.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the method used for the server.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the plugin executable filename.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Plugin { get; set; }

        /// <summary>
        /// Gets or sets the plugin options passed as environment variables.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? PluginOpts { get; set; }

        /// <summary>
        /// Gets or sets the server name.
        /// </summary>
        [JsonPropertyName("remarks")]
        public string Name { get; set; }
    }
}
