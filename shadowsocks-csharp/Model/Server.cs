using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Server
    {
        public static readonly Regex
            UrlFinder = new Regex(@"ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\S+))?", RegexOptions.IgnoreCase),
            DetailsParser = new Regex(@"^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\d+?))$", RegexOptions.IgnoreCase);

        private const int DefaultServerTimeoutSec = 5;
        public const int MaxServerTimeoutSec = 20;

        public string server;
        public int server_port;
        public string password;
        public string method;
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
            password = "";
            remarks = "";
            timeout = DefaultServerTimeoutSec;
        }

        public static List<Server> GetServers(string ssURL)
        {
            var matches = UrlFinder.Matches(ssURL);
            if (matches.Count <= 0) return null;
            List<Server> servers = new List<Server>();
            foreach (Match match in matches)
            {
                Server tmp = new Server();
                var base64 = match.Groups["base64"].Value;
                var tag = match.Groups["tag"].Value;
                if (!tag.IsNullOrEmpty())
                {
                    tmp.remarks = HttpUtility.UrlDecode(tag, Encoding.UTF8);
                }
                Match details = DetailsParser.Match(Encoding.UTF8.GetString(Convert.FromBase64String(
                    base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='))));
                if (!details.Success)
                    continue;
                tmp.method = details.Groups["method"].Value;
                tmp.password = details.Groups["password"].Value;
                tmp.server = details.Groups["hostname"].Value;
                tmp.server_port = int.Parse(details.Groups["port"].Value);

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
