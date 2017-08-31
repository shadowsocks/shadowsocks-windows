using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Server
    {
        private const int DefaultServerTimeoutSec = 5;
        public const int MaxServerTimeoutSec = 20;

        public string server;
        public int server_port;
        public string password;
        public string method;
        public string plugin;
        public string plugin_opts;
        public string remarks;
        public int timeout;

        public override int GetHashCode()
        {
            return server.GetHashCode() ^ server_port;
        }

        public override bool Equals(object obj)
        {
            Server o2 = (Server)obj;
            return server == o2.server && server_port == o2.server_port;
        }

        public string FriendlyName()
        {
            if (server.IsNullOrEmpty())
            {
                return I18N.GetString("New server");
            }
            string serverStr;
            // CheckHostName() won't do a real DNS lookup
            var hostType = Uri.CheckHostName(server);

            switch (hostType)
            {
                case UriHostNameType.IPv6:
                    serverStr = $"[{server}]:{server_port}";
                    break;
                default:
                    // IPv4 and domain name
                    serverStr = $"{server}:{server_port}";
                    break;
            }
            return remarks.IsNullOrEmpty()
                ? serverStr
                : $"{remarks} ({serverStr})";
        }

        public Server()
        {
            server = "";
            server_port = 8388;
            method = "aes-256-cfb";
            plugin = "";
            plugin_opts = "";
            password = "";
            remarks = "";
            timeout = DefaultServerTimeoutSec;
        }

        public static List<Server> GetServers(string ssURL)
        {
            var serverUrls = ssURL.Split('\r', '\n');

            List<Server> servers = new List<Server>();
            foreach (string serverUrl in serverUrls)
            {
                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    continue;
                }

                Uri parsedUrl;
                try
                {
                    parsedUrl = new Uri(serverUrl);
                }
                catch (UriFormatException)
                {
                    continue;
                }

                Server tmp = new Server
                {
                    remarks = parsedUrl.GetComponents(UriComponents.Fragment, UriFormat.Unescaped)
                };

                string possiblyUnpaddedBase64 = parsedUrl.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
                bool isOldFormatUrl = possiblyUnpaddedBase64.Length == 0;
                if (isOldFormatUrl)
                {
                    int prefixLength = "ss://".Length;
                    int indexOfHashOrSlash = serverUrl.LastIndexOfAny(
                        new[] { '/', '#' },
                        serverUrl.Length - 1,
                        serverUrl.Length - prefixLength);

                    int substringLength = serverUrl.Length - prefixLength;
                    if (indexOfHashOrSlash >= 0)
                    {
                        substringLength = indexOfHashOrSlash - prefixLength;
                    }

                    possiblyUnpaddedBase64 = serverUrl.Substring(prefixLength, substringLength).TrimEnd('/');
                }
                else
                {
                    // Web-safe base64 to normal base64
                    possiblyUnpaddedBase64 = possiblyUnpaddedBase64.Replace('-', '+').Replace('_', '/');
                }

                string base64 = possiblyUnpaddedBase64.PadRight(
                    possiblyUnpaddedBase64.Length + (4 - possiblyUnpaddedBase64.Length % 4) % 4,
                    '=');

                string innerUserInfoOrUrl = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                string userInfo;
                if (isOldFormatUrl)
                {
                    Uri innerUrl = new Uri("inner://" + innerUserInfoOrUrl);
                    userInfo = innerUrl.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
                    tmp.server = innerUrl.GetComponents(UriComponents.Host, UriFormat.Unescaped);
                    tmp.server_port = innerUrl.Port;
                }
                else
                {
                    userInfo = innerUserInfoOrUrl;
                    tmp.server = parsedUrl.GetComponents(UriComponents.Host, UriFormat.Unescaped);
                    tmp.server_port = parsedUrl.Port;
                }

                string[] userInfoParts = userInfo.Split(new[] { ':' }, 2);
                if (userInfoParts.Length != 2)
                {
                    continue;
                }

                tmp.method = userInfoParts[0];
                tmp.password = userInfoParts[1];

                NameValueCollection queryParameters = HttpUtility.ParseQueryString(parsedUrl.Query);
                string[] pluginParts = HttpUtility.UrlDecode(queryParameters["plugin"] ?? "").Split(new[] { ';' }, 2);
                if (pluginParts.Length > 0)
                {
                    tmp.plugin = pluginParts[0] ?? "";
                }

                if (pluginParts.Length > 1)
                {
                    tmp.plugin_opts = pluginParts[1] ?? "";
                }

                servers.Add(tmp);
            }
            return servers;
        }

        public string Identifier()
        {
            return server + ':' + server_port;
        }
    }
}
