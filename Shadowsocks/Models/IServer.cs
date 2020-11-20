using System;
using System.Text.Json.Serialization;

namespace Shadowsocks.Models
{
    public interface IServer : IEquatable<IServer>
    {
        [JsonPropertyName("server")]
        public string Host { get; set; }
        [JsonPropertyName("server_port")]
        public int Port { get; set; }
        public string Password { get; set; }
        public string Method { get; set; }
        public string Plugin { get; set; }
        public string PluginOpts { get; set; }
        [JsonPropertyName("remarks")]
        public string Name { get; set; }
    }
}
