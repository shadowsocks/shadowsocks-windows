using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Shadowsocks.Models
{
    [Serializable]
    public class Server
    {
        public string server;
        public int server_port;
        public string password;
        public string method;
        public string remarks;
        public bool auth;

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
            if (string.IsNullOrEmpty(server))
            {
                return "New server";
            }
            if (string.IsNullOrEmpty(remarks))
            {
                return server + ":" + server_port;
            }
            return remarks + " (" + server + ":" + server_port + ")";
        }

        public Server()
        {
            server = "";
            server_port = 8388;
            method = "aes-256-cfb";
            password = "";
            remarks = "";
            auth = false;
        }

        public Server(string ssURL) : this()
        {
            string[] r1 = Regex.Split(ssURL, "ss://", RegexOptions.IgnoreCase);
            string base64 = r1[1];
            byte[] bytes = null;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    bytes = Convert.FromBase64String(base64);
                    break;
                }
                catch (FormatException)
                {
                    base64 += "=";
                }
            }
            if (bytes == null)
            {
                throw new FormatException();
            }
            try
            {
                string data = Encoding.UTF8.GetString(bytes);
                int indexLastAt = data.LastIndexOf('@');

                string afterAt = data.Substring(indexLastAt + 1);
                int indexLastColon = afterAt.LastIndexOf(':');
                server_port = int.Parse(afterAt.Substring(indexLastColon + 1));
                server = afterAt.Substring(0, indexLastColon);

                string beforeAt = data.Substring(0, indexLastAt);
                string[] parts = beforeAt.Split(':');
                method = parts[0];
                password = beforeAt.Remove(0, method.Length + 1);

                //TODO: read one_time_auth
            }
            catch (IndexOutOfRangeException)
            {
                throw new FormatException();
            }
        }


        private static readonly Regex isbase64 = new Regex("[a-zA-z0-9=]+", RegexOptions.Compiled);
        public static Server[] ParseMultipleServers(string input)
        {
            var svcs = new List<Server>();
            try
            {
                var r1 = Regex.Split(input, "ss://", RegexOptions.IgnoreCase);
                foreach (var s in r1)
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    var momoda = isbase64.Match(s);
                    if (!momoda.Success) continue;
                    try
                    {
                        var prs = new Server($"ss://{momoda.Value}");
                        if (!svcs.Contains(prs))
                        {
                            svcs.Add(prs);
                        }
                    }
                    catch
                    {
                        //whaaaaaat?
                    }
                }
            }
            catch
            {
                //whaaaaaat?
            }
            return svcs.Count == 0 ? null : svcs.ToArray();
        }

        public string Identifier()
        {
            return server + ':' + server_port;
        }
    }
}
