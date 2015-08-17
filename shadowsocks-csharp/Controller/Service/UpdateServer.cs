using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Shadowsocks.Controller
{
    public class UpdateServer
    {
        private WebClient http;
        private List<Server> servers;
        public UpdateServer(Configuration config)
        {
            http = new WebClient();
            http.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36");
            //http.Proxy = new WebProxy(IPAddress.Loopback.ToString(), config.localPort);
            servers = new List<Server>();
        }

        public List<Server> updateServerData()
        {
            byte[] bytes = http.DownloadData("http://www.ishadowsocks.com/");
            string str = System.Text.Encoding.GetEncoding("utf-8").GetString(bytes);
            MatchCollection mc = Regex.Matches(str, @"<div class=""col-lg-4 text-center"">\s*(<h4>.*?</h4>\s*)+</div>");

            foreach (Match match in mc)
            {
                string serverStr = match.ToString();
                MatchCollection serverMc = Regex.Matches(serverStr, @"<h4>(.*?)</h4>");
                Server server = Configuration.GetDefaultServer();
                foreach (Match m in serverMc)
                {
                    string value = m.Groups[1].Value;
                    string[] values = value.Split(':');
                    if (values.Length == 2)
                    {
                        if (values[0].Contains("服务器地址"))
                        {
                            server.remarks = values[0];
                            server.server = values[1];
                        }
                        if (values[0].Contains("端口"))
                        {
                            server.server_port = Convert.ToInt32(values[1]);
                        }
                        if (values[0].Contains("密码") && !values[0].Contains("注意"))
                        {
                            server.password = values[1];
                        }
                        if (values[0].Contains("加密方式"))
                        {
                            server.method = values[1];
                            servers.Add(server);
                        }
                    }
                }
            }
            return servers;
        }
    }
}
