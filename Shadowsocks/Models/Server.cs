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
            Port = 8388;
            Password = "";
            Method = "chacha20-ietf-poly1305";
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

        public override bool Equals(object? obj) => obj is Server server && Uuid == server.Uuid;
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Name;

        /// <summary>
        /// Converts this server object into an ss:// URL.
        /// </summary>
        /// <returns></returns>
        public Uri ToUrl()
        {
            UriBuilder uriBuilder = new UriBuilder("ss", Host, Port)
            {
                UserName = Utilities.Base64Url.Encode($"{Method}:{Password}"),
                Fragment = Name,
            };
            if (!string.IsNullOrEmpty(Plugin))
                if (!string.IsNullOrEmpty(PluginOpts))
                    uriBuilder.Query = $"plugin={Uri.EscapeDataString($"{Plugin};{PluginOpts}")}"; // manually escape as a workaround
                else
                    uriBuilder.Query = $"plugin={Plugin}";
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Tries to parse an ss:// URL into a Server object.
        /// </summary>
        /// <param name="url">The ss:// URL to parse.</param>
        /// <param name="server">
        /// A Server object represented by the URL.
        /// A new empty Server object if the URL is invalid.</param>
        /// <returns>True for success. False for failure.</returns>
        public static bool TryParse(string url, out Server server)
        {
            try
            {
                var uri = new Uri(url);
                if (uri.Scheme != "ss")
                    throw new ArgumentException("Wrong URL scheme");
                var userinfo_base64url = uri.UserInfo;
                var userinfo = Utilities.Base64Url.DecodeToString(userinfo_base64url);
                var userinfoSplitArray = userinfo.Split(':', 2);
                var method = userinfoSplitArray[0];
                var password = userinfoSplitArray[1];
                server = new Server(uri.Fragment, new Guid().ToString(), uri.Host, uri.Port, password, method);
                // find the plugin query
                var parsedQueriesArray = uri.Query.Split("?&");
                var pluginQueryContent = "";
                foreach (var query in parsedQueriesArray)
                {
                    if (query.StartsWith("plugin=") && query.Length > 7)
                    {
                        pluginQueryContent = query[7..]; // remove "plugin="
                    }
                }
                var unescapedpluginQuery = Uri.UnescapeDataString(pluginQueryContent);
                var parsedPluginQueryArray = unescapedpluginQuery.Split(';', 2);
                if (parsedPluginQueryArray.Length == 2) // is valid plugin query
                {
                    server.Plugin = parsedPluginQueryArray[0];
                    server.PluginOpts = parsedPluginQueryArray[1];
                }
                return true;
            }
            catch
            {
                server = new Server();
                return false;
            }
        }
    }
}
