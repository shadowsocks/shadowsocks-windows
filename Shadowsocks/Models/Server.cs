using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Shadowsocks.Models
{
    public class Server : IServer
    {
        /// <inheritdoc/>
        [JsonPropertyName("server")]
        public string Host { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("server_port")]
        public int Port { get; set; }

        /// <inheritdoc/>
        public string Password { get; set; }

        /// <inheritdoc/>
        public string Method { get; set; }

        /// <inheritdoc/>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Plugin { get; set; }

        /// <inheritdoc/>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? PluginOpts { get; set; }

        /// <summary>
        /// Gets or sets the arguments passed to the plugin process.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<string>? PluginArgs { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("remarks")]
        public string Name { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("id")]
        public string Uuid { get; set; }

        public Server()
        {
            Host = "";
            Port = 8388;
            Password = "";
            Method = "chacha20-ietf-poly1305";
            Name = "";
            Uuid = Guid.NewGuid().ToString();
        }

        public bool Equals(IServer? other) => other is Server anotherServer && Uuid == anotherServer.Uuid;
        public override int GetHashCode() => Uuid.GetHashCode();
        public override string ToString() => Name;

        /// <summary>
        /// Converts this server object into an ss:// URL.
        /// </summary>
        /// <returns></returns>
        public Uri ToUrl()
        {
            UriBuilder uriBuilder = new("ss", Host, Port)
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
        /// A new empty Server object if the URL is invalid.
        /// </param>
        /// <returns>True for success. False for failure.</returns>
        public static bool TryParse(string url, [NotNullWhen(true)] out Server? server)
        {
            server = null;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && TryParse(uri, out server);
        }

        /// <summary>
        /// Tries to parse an ss:// URL into a Server object.
        /// </summary>
        /// <param name="uri">The ss:// URL to parse.</param>
        /// <param name="server">
        /// A Server object represented by the URL.
        /// A new empty Server object if the URL is invalid.
        /// </param>
        /// <returns>True for success. False for failure.</returns>
        public static bool TryParse(Uri uri, [NotNullWhen(true)] out Server? server)
        {
            server = null;
            try
            {
                if (uri.Scheme != "ss")
                    return false;
                var userinfo_base64url = uri.UserInfo;
                var userinfo = Utilities.Base64Url.DecodeToString(userinfo_base64url);
                var userinfoSplitArray = userinfo.Split(':', 2);
                var method = userinfoSplitArray[0];
                var password = userinfoSplitArray[1];
                var host = uri.HostNameType == UriHostNameType.IPv6 ? uri.Host[1..^1] : uri.Host;
                var escapedFragment = string.IsNullOrEmpty(uri.Fragment) ? uri.Fragment : uri.Fragment[1..];
                var name = Uri.UnescapeDataString(escapedFragment);
                server = new Server()
                {
                    Name = name,
                    Uuid = Guid.NewGuid().ToString(),
                    Host = host,
                    Port = uri.Port,
                    Password = password,
                    Method = method,
                };
                // find the plugin query
                var parsedQueriesArray = uri.Query.Split('?', '&');
                var pluginQueryContent = "";
                foreach (var query in parsedQueriesArray)
                {
                    if (query.StartsWith("plugin=") && query.Length > 7)
                    {
                        pluginQueryContent = query[7..]; // remove "plugin="
                    }
                }
                if (string.IsNullOrEmpty(pluginQueryContent)) // no plugin
                    return true;
                var unescapedpluginQuery = Uri.UnescapeDataString(pluginQueryContent);
                var parsedPluginQueryArray = unescapedpluginQuery.Split(';', 2);
                if (parsedPluginQueryArray.Length == 1)
                {
                    server.Plugin = parsedPluginQueryArray[0];
                }
                else if (parsedPluginQueryArray.Length == 2) // is valid plugin query
                {
                    server.Plugin = parsedPluginQueryArray[0];
                    server.PluginOpts = parsedPluginQueryArray[1];
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
