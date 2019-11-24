using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using Shadowsocks.Controller;
using System.Text.RegularExpressions;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Server
    {
        #region ParseLegacyURL
        public static readonly Regex
            UrlFinder = new Regex(@"ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\S+))?", RegexOptions.IgnoreCase),
            DetailsParser = new Regex(@"^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\d+?))$", RegexOptions.IgnoreCase);
        #endregion ParseLegacyURL

        private const int DefaultServerTimeoutSec = 5;
        public const int MaxServerTimeoutSec = 20;

        public string server;
        public int server_port;
        public int server_udp_port;
        public string password;
        public string method;
        public string protocol;
        public string protocolparam;
        public string plugin;
        public string plugin_opts;
        public string plugin_args;
        public string remarks;
        public int timeout;
        public string group;
        public bool udp_over_tcp;
        public string obfs;
        public string obfsparam;

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

            string serverStr = $"{FormatHostName(server)}:{server_port}";
            return remarks.IsNullOrEmpty()
                ? serverStr
                : $"{remarks} ({serverStr})";
        }

        public string FormatHostName(string hostName)
        {
            // CheckHostName() won't do a real DNS lookup
            switch (Uri.CheckHostName(hostName))
            {
                case UriHostNameType.IPv6:  // Add square bracket when IPv6 (RFC3986)
                    return $"[{hostName}]";
                default:    // IPv4 or domain name
                    return hostName;
            }
        }

        public Server()
        {
            server = "";
            server_port = 8388;
            method = "aes-256-cfb";
            plugin = "";
            plugin_opts = "";
            plugin_args = "";
            password = "";
            remarks = "";
            timeout = DefaultServerTimeoutSec;
        }

        private static Server ParseLegacyURL(string ssURL)
        {
            var match = UrlFinder.Match(ssURL);
            if (!match.Success)
                return null;

            Server server = new Server();
            var base64 = match.Groups["base64"].Value.TrimEnd('/');
            var tag = match.Groups["tag"].Value;
            if (!tag.IsNullOrEmpty())
            {
                server.remarks = HttpUtility.UrlDecode(tag, Encoding.UTF8);
            }
            Match details = null;
            try
            {
                details = DetailsParser.Match(Encoding.UTF8.GetString(Convert.FromBase64String(
                base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='))));
            }
            catch (FormatException)
            {
                return null;
            }
            if (!details.Success)
                return null;
            server.method = details.Groups["method"].Value;
            server.password = details.Groups["password"].Value;
            server.server = details.Groups["hostname"].Value;
            server.server_port = int.Parse(details.Groups["port"].Value);
            return server;
        }

        public static List<Server> GetServers(string ssURL)
        {
            var serverUrls = ssURL.Split('\r', '\n', ' ');

            List<Server> servers = new List<Server>();
            foreach (string serverUrl in serverUrls)
            {
                string _serverUrl = serverUrl.Trim();
                if (_serverUrl.StartsWith("ss://", StringComparison.OrdinalIgnoreCase))
                {
                    var server = ServerFromSS(_serverUrl);
                    if(server!=null) servers.Add(server); ;
                }
                else if (_serverUrl.StartsWith("ssr://", StringComparison.OrdinalIgnoreCase))
                {
                    var server =   ServerFromSSR(_serverUrl);
                    if(server!=null) servers.Add(server); ;
                }
                else continue;
            }
            return servers;
        }

        private static Server ServerFromSS(string ssURL, string force_group="")
        {
            var result=new Server();
            Regex UrlFinder = new Regex("^(?i)ss://([A-Za-z0-9+-/=_]+)(#(.+))?", RegexOptions.IgnoreCase),
                DetailsParser = new Regex("^((?<method>.+):(?<password>.*)@(?<hostname>.+?)" +
                                          ":(?<port>\\d+?))$", RegexOptions.IgnoreCase);

            var match = UrlFinder.Match(ssURL);
            if (!match.Success)
                return null;

            var base64 = match.Groups[1].Value;
            match = DetailsParser.Match(Encoding.UTF8.GetString(Convert.FromBase64String(
                base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='))));
            result.protocol = "origin";
            result.method = match.Groups["method"].Value;
            result.password = match.Groups["password"].Value;
            result.server = match.Groups["hostname"].Value;
            result.server_port = int.Parse(match.Groups["port"].Value);
            if (!String.IsNullOrEmpty(force_group))
                result.group = force_group;
            else
                result.group = "";
            return result;
        }

         private static Server ServerFromSSR(string ssrURL, string force_group="")
        {
            var result=new Server();
            // ssr://host:port:protocol:method:obfs:base64pass/?obfsparam=base64&remarks=base64&group=base64&udpport=0&uot=1
            Match ssr = Regex.Match(ssrURL, "ssr://([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
            if (!ssr.Success)
                return null;

            string data = Util.Base64.DecodeUrlSafeBase64(ssr.Groups[1].Value);
            Dictionary<string, string> params_dict = new Dictionary<string, string>();

            Match match = null;
            for (int nTry = 0; nTry < 2; ++nTry)
            {
                int param_start_pos = data.IndexOf("?");
                if (param_start_pos > 0)
                {
                    params_dict = ParseParam(data.Substring(param_start_pos + 1));
                    data = data.Substring(0, param_start_pos);
                }
                if (data.IndexOf("/") >= 0)
                {
                    data = data.Substring(0, data.LastIndexOf("/"));
                }

                Regex UrlFinder = new Regex("^(.+):([^:]+):([^:]*):([^:]+):([^:]*):([^:]+)");
                match = UrlFinder.Match(data);
                if (match.Success)
                    break;
                // try match which not encode to base64
                //ssr = Regex.Match(ssrURL, @"ssr://([A-Za-z0-9-_.:=?&/\[\]]+)", RegexOptions.IgnoreCase);
                //if (ssr.Success)
                //    data = ssr.Groups[1].Value;
                //else
                    throw new FormatException();
            }
            if (match == null || !match.Success)
                throw new FormatException();

            result.server = match.Groups[1].Value;
            result.server_port = int.Parse(match.Groups[2].Value);
            result.protocol = match.Groups[3].Value.Length == 0 ? "origin" : match.Groups[3].Value;
            result.protocol = result.protocol.Replace("_compatible", "");
            result.method = match.Groups[4].Value;
            result.obfs = match.Groups[5].Value.Length == 0 ? "plain" : match.Groups[5].Value;
            result.obfs = result.obfs.Replace("_compatible", "");
            result.password = Util.Base64.DecodeStandardSSRUrlSafeBase64(match.Groups[6].Value);

            if (params_dict.ContainsKey("protoparam"))
            {
                result.protocolparam = Util.Base64.DecodeStandardSSRUrlSafeBase64(params_dict["protoparam"]);
            }
            if (params_dict.ContainsKey("obfsparam"))
            {
                result.obfsparam = Util.Base64.DecodeStandardSSRUrlSafeBase64(params_dict["obfsparam"]);
            }
            if (params_dict.ContainsKey("remarks"))
            {
                result.remarks = Util.Base64.DecodeStandardSSRUrlSafeBase64(params_dict["remarks"]);
            }
            if (params_dict.ContainsKey("group"))
            {
                result.group = Util.Base64.DecodeStandardSSRUrlSafeBase64(params_dict["group"]);
            }
            else
                result.group = "";
            if (params_dict.ContainsKey("uot"))
            {
                result.udp_over_tcp = int.Parse(params_dict["uot"]) != 0;
            }
            if (params_dict.ContainsKey("udpport"))
            {
                result.server_udp_port = int.Parse(params_dict["udpport"]);
            }
            if (!String.IsNullOrEmpty(force_group))
                result.group = force_group;
            return result;
        }

         private static Dictionary<string, string> ParseParam(string param_str)
         {
             Dictionary<string, string> params_dict = new Dictionary<string, string>();
             string[] obfs_params = param_str.Split('&');
             foreach (string p in obfs_params)
             {
                 if (p.IndexOf('=') > 0)
                 {
                     int index = p.IndexOf('=');
                     string key, val;
                     key = p.Substring(0, index);
                     val = p.Substring(index + 1);
                     params_dict[key] = val;
                 }
             }
             return params_dict;
         }

        public string Identifier()
        {
            return server + ':' + server_port;
        }
    }
}
