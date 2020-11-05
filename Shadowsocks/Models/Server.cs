using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Shadowsocks.Models
{
    public class Server
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
        [JsonPropertyName("id")]
        public string Uuid { get; set; }

        public Server()
        {
            Host = "";
            Password = "";
            Method = "";
            Plugin = "";
            PluginOpts = "";
            Name = "";
            Uuid = "";
        }

        public Server(
            string name,
            string uuid,
            string host,
            int port,
            string password,
            string method,
            string plugin = "",
            string pluginOpts = "")
        {
            Host = host;
            Port = port;
            Password = password;
            Method = method;
            Plugin = plugin;
            PluginOpts = pluginOpts;
            Name = name;
            Uuid = uuid;
        }
    }
}
