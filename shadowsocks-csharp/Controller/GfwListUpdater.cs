using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using Shadowsocks.Properties;
using SimpleJson;
using Shadowsocks.Util;
using Shadowsocks.Model;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shadowsocks.Controller
{
    public class GFWListUpdater
    {
        private const string GFWLIST_URL = "https://autoproxy-gfwlist.googlecode.com/svn/trunk/gfwlist.txt";

        private static string PAC_FILE = PACServer.PAC_FILE;

        private static string USER_RULE_FILE = PACServer.USER_RULE_FILE;

        public event EventHandler<ResultEventArgs> UpdateCompleted;

        public event ErrorEventHandler Error;

        public class ResultEventArgs : EventArgs
        {
            public bool Success;

            public ResultEventArgs(bool success)
            {
                this.Success = success;
            }
        }

        private void savePacFile(string pacContent)
        {
            try
            {
                if (File.Exists(PAC_FILE))
                {
                    string original = File.ReadAllText(PAC_FILE, Encoding.UTF8);
                    if (original == pacContent)
                    {
                        UpdateCompleted(this, new ResultEventArgs(false));
                        return;
                    }
                }
                File.WriteAllText(PAC_FILE, pacContent, Encoding.UTF8);
                if (UpdateCompleted != null)
                {
                    UpdateCompleted(this, new ResultEventArgs(true));
                }
            }
            catch (Exception ex)
            {
                if (Error != null)
                {
                    Error(this, new ErrorEventArgs(ex));
                }
            }
        }

        private string getHostname(string domain)
        {
            string host = null;

            if (!(domain.StartsWith("http:") || domain.StartsWith("https:")))
                domain = "http://" + domain;

            Uri hostUri;
            if (Uri.TryCreate(domain, UriKind.Absolute, out hostUri))
                host = hostUri.Host;

            return host;
        }

        private void addDomainToList(List<string> list, string domain)
        {
            string hostname = getHostname(domain);
            if (hostname != null)
                list.Add(hostname);
        }

        private List<string> parseGfwlist(List<string> lines)
        {
            List<string> domains = new List<string>();
            lines.ForEach(delegate(string line)
            {
                if (line.StartsWith("@@") || line.StartsWith("/") || Regex.IsMatch(line, "^(\\d+\\.){3}\\d+$") || !line.Contains("."))
                    return;

                if (line.Contains("//*"))
                    line = line.Replace("//*", "//");
                else if (line.Contains("*/") || line.Contains("*."))
                    line = line.Replace("*", "");
                else if (line.Contains("*"))
                    line = line.Replace("*", "/");

                if (line.StartsWith("|"))
                    line = line.TrimStart('|');
                else if (line.StartsWith("."))
                    line = line.TrimStart('.');

                addDomainToList(domains, line);
            });
            return domains;
        }

        private List<string> reduceDomains(List<string> domains)
        {
            HashSet<string> uniDomains = new HashSet<string>();
            foreach (var domain in domains)
            {
                var domainParts = domain.Split('.');
                bool isContainRootDomain = false;

                for (var i = 0; i <= domainParts.Length - 2; i++)
                {
                    var domainPartsArray = new ArraySegment<string>(domainParts, domainParts.Length - i - 2, i + 2);
                    var rootDomain = string.Join(".", domainPartsArray);

                    if (domains.Contains(rootDomain)) 
                    {
                        uniDomains.Add(rootDomain);
                        isContainRootDomain = true;
                        break;
                    }
                }
                if (!isContainRootDomain)
                    uniDomains.Add(domain);
            }
            return uniDomains.ToList();
        }

        private void generatePacFast(List<string> lines)
        {
            List<string> domains = parseGfwlist(lines);
            domains = reduceDomains(domains).Distinct().ToList();
            var fastContent = Utils.UnGzip(Resources.proxy_pac);
            var domainsJsonStr = "{\n    " + 
                string.Join(",\n    ", domains.Select(d=>string.Format("\"{0}\": 1", d))) + 
                "\n}";
            fastContent = fastContent.Replace("__DOMAINS__", domainsJsonStr);
            savePacFile(fastContent);
        }

        private void generatePacPrecise(List<string> lines)
        {
            string abpContent = Utils.UnGzip(Resources.abp_js);
            abpContent = abpContent.Replace("__RULES__", SimpleJson.SimpleJson.SerializeObject(lines));
            savePacFile(abpContent);
        }

        private void  parsePacStr(string pacStr, List<string> lines)
        {
            string[] rules = pacStr.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rule in rules)
            {
                if (rule.StartsWith("!") || rule.StartsWith("["))
                    continue;
                lines.Add(rule);
            }
        }

        private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                List<string> lines = ParseResult(e.Result);
                
                string local = "";
                if (File.Exists(USER_RULE_FILE))
                {
                    local = File.ReadAllText(USER_RULE_FILE, Encoding.UTF8);
                }

                bool usePreciseMode = (bool) e.UserState;
                
                if (!usePreciseMode) 
                {
                    local += Utils.UnGzip(Resources.builtin_txt);
                }
                parsePacStr(local, lines);

                if (usePreciseMode)
                {
                    generatePacPrecise(lines);
                }
                else
                {
                    generatePacFast(lines);
                }
            }
            catch (Exception ex)
            {
                if (Error != null)
                {
                    Error(this, new ErrorEventArgs(ex));
                }
            }
        }

        public void UpdatePACFromGFWList(Configuration config)
        {
            WebClient http = new WebClient();
            http.Proxy = new WebProxy(IPAddress.Loopback.ToString(), config.localPort);
            http.DownloadStringCompleted += http_DownloadStringCompleted;
            http.DownloadStringAsync(new Uri(GFWLIST_URL), config.usePreciseMode);
        }

        public List<string> ParseResult(string response)
        {
            byte[] bytes = Convert.FromBase64String(response);
            string content = Encoding.ASCII.GetString(bytes);
            string[] lines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> valid_lines = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                if (line.StartsWith("!") || line.StartsWith("["))
                    continue;
                valid_lines.Add(line);
            }
            return valid_lines;
        }
    }
}