using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    public class GFWListUpdater
    {
        private static readonly string _GFWLIST_URL = "https://raw.githubusercontent.com/gfwlist/gfwlist/master/gfwlist.txt";
        private static readonly string _IPV4_PATTERN = @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}(:[0-9]{1,5})?$";

        public event EventHandler<ResultEventArgs> OnUpdateCompleted;
        public event ErrorEventHandler OnError;
        private Configuration _config;

        public enum PACFileMode
        {
            Precise = 1,    // Adblock Plus precise PAC
            Fast = 2        // fast PAC which uses O(1) domain only lookup
        }

        public class ResultEventArgs : EventArgs
        {
            public bool Success;
            public ResultEventArgs(bool success) { Success = success; }
        }

        /// <summary>
        /// Download `GFW List` from internet async.
        /// </summary>
        /// <param name="config"></param>
        public void UpdatePACFromGFWList(Configuration config)
        {
            _config = config;
            WebClient http = new WebClient();
            http.Proxy = new WebProxy(IPAddress.Loopback.ToString(), _config.localPort);
            http.DownloadStringCompleted += GFWListDownloadCompleted;
            http.DownloadStringAsync(new Uri(_GFWLIST_URL));
        }

        /// <summary>
        /// Occurs on `GFW List` download.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GFWListDownloadCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var gfwlistBase64Text = e.Result;
                Utils.WriteAllText(Utils.GetTempPath("gfwlist.txt"), gfwlistBase64Text, Encoding.ASCII);
                PACFileHandler(gfwlistBase64Text, _config.pacFileMode);
                OnUpdateCompleted?.Invoke(this, new ResultEventArgs(true));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        /// <summary>
        /// Create the file <paramref name="PACServer.PAC_FILE"/> which contains the `GFW List` and
        /// `User Rules` in specified PAC file mode.
        /// </summary>
        /// <param name="gfwlistBase64Text">The GFW List file content in Base64 encoded.</param>
        /// <param name="mode">Specify the PAC file mode.</param>
        public void PACFileHandler(string gfwlistBase64Text, PACFileMode mode)
        {
            var gfwlistText = DecodeBase64Text(gfwlistBase64Text);
            var userRuleText = File.Exists(PACServer.USER_RULE_FILE)
                             ? File.ReadAllText(PACServer.USER_RULE_FILE, Encoding.UTF8)
                             : "";
            string pacContent = "";
            switch (mode)
            {
                case PACFileMode.Precise:
                    {
                        var rules = gfwlistText + Environment.NewLine
                                  + userRuleText;
                        var pacFileTpl = File.Exists(PACServer.USER_ABP_FILE)
                                       ? File.ReadAllText(PACServer.USER_ABP_FILE, Encoding.UTF8)
                                       : Utils.UnGzip(Resources.PACFileTplABP);
                        var ruleLines = RemoveCommentLines(rules);
                        pacContent = pacFileTpl.Replace("__RULES__", JsonConvert.SerializeObject(ruleLines, Formatting.Indented));
                    }
                    break;
                case PACFileMode.Fast:
                    {
                        var builtinRuleText = Utils.UnGzip(Resources.BuiltinRules);
                        var rules = builtinRuleText + Environment.NewLine
                                  + gfwlistText + Environment.NewLine
                                  + userRuleText;
                        var pacFileTpl = Utils.UnGzip(Resources.PACFileTplFastMode);
                        var ruleLines = RemoveCommentLines(rules);
                        var domains = CompactDomainNames(PickDomainNames(ruleLines));
                        var dictStr = "";
                        foreach (var domain in domains)
                        {
                            dictStr += $"\t\"{domain}\": 1,{Environment.NewLine}";
                        }
                        dictStr = "{" + Environment.NewLine
                                + dictStr.Substring(0, dictStr.Length - 3) + Environment.NewLine
                                + "}";
                        pacContent = pacFileTpl.Replace("__DOMAINS__", dictStr);
                    }
                    break;
                default:
                    throw new ArithmeticException("This code should never be touched. " +
                                                  "Please issue to community @github.");
            }
            Utils.WriteAllText(PACServer.PAC_FILE, pacContent, Encoding.UTF8);
        }

        /// <summary>
        /// Decode a Base64 encoded text and return in plan text.
        /// </summary>
        /// <param name="base64Text">A Base64 encoded text.</param>
        /// <returns>A decoded Base64 encoded text.</returns>
        private static string DecodeBase64Text(string base64Text)
        {
            var bytes = Convert.FromBase64String(base64Text);
            var result = Encoding.ASCII.GetString(bytes);
            return result;
        }

        /// <summary>
        /// Remove comment lines from Adblock Plus like contents.
        /// </summary>
        /// <param name="contents">Adblock Plus like contents.</param>
        /// <returns>A string array of Adblock Plus like rules</returns>
        private static string[] RemoveCommentLines(string contents)
        {
            var lines = contents.Split(new string[] { "\r", "\n" },
                                       StringSplitOptions.RemoveEmptyEntries);
            var result = new HashSet<string>();
            foreach (var line in lines)
                if (!line.StartsWith("!") && !line.StartsWith("["))
                    // comment line starts with "!" or "["
                    result.Add(line);
            return result.ToArray();
        }

        /// <summary>
        /// Pick out domain names from a array of Adblock Plus like rules,
        /// i.e. 'some.host.example.org' => 'example.org'.
        /// </summary>
        /// <param name="rules">A string array of Adblock Plus like rules</param>
        /// <returns>A string array of domain names.</returns>
        private string[] PickDomainNames(string[] rules)
        {
            var ipaddrs = new HashSet<string>();
            var domains = new HashSet<string>();
            string str;
            foreach (var rule in rules)
            {
                if (rule.StartsWith("@@"))
                {
                    // drop white list
                    Logging.Info($"Dropped line \"{rule}\", because of white list.");
                    continue;
                }
                else if (rule.StartsWith("/") && rule.EndsWith("/"))
                {
                    // drop regular expression
                    Logging.Info($"Dropped line \"{rule}\", because of regular expression.");
                    continue;
                }
                str = rule.Replace("%2F", "/");
                if (str.StartsWith("||"))
                    str = str.Substring(2);
                else if (str.StartsWith("|"))
                    str = str.Substring(1);
                if (str.StartsWith("http://"))
                    str = str.Substring(7);
                else if (str.StartsWith("https://"))
                    str = str.Substring(8);
                var domain = str.Split('/')[0];         // "host.example.org/a/path" => "host.example.org"
                if (Regex.Match(domain, _IPV4_PATTERN).Success)
                {
                    ipaddrs.Add(domain);
                    continue;
                }
                var domainParts = domain.Split('.').ToList();
                if (domainParts.First().Contains("*"))  // "ab*cd.example.org" => "example.org"
                    domain = string.Join(".", domainParts.GetRange(1, domainParts.Count - 1));
                if (domainParts.Last().Contains("*"))   // "example.org*abcde" => "example.org"
                    domain = domain.Split('*')[0];
                if (domain.Contains("*"))
                {
                    Logging.Info($"Dropped \"{rule}\", because of \"*\" in domain.");
                    continue;
                }
                else if (domain.StartsWith("q="))
                {
                    Logging.Info($"Dropped \"{rule}\", because of \"q=\" in domain.");
                    continue;
                }
                else if (domain.Contains("%"))
                {
                    Logging.Info($"Dropped \"{rule}\", because of \"%\" in domain.");
                    continue;
                }
                else if (domain == "")
                {
                    Logging.Info($"Dropped \"{rule}\", because of empty domain.");
                    continue;
                }
                else if (domain.EndsWith("."))
                {
                    Logging.Info($"Dropped \"{rule}\", because of \".\" ending domain.");
                    continue;
                }
                else if (!domain.Contains("."))
                {
                    Logging.Info($"Dropped \"{rule}\", because of no \".\" in domain.");
                    continue;
                }
                else if (domain.StartsWith("www."))
                    domain = domain.Substring(4);       // "www.example.org" => "example.org"
                else if (domain.StartsWith("."))
                    domain = domain.Substring(1);       // ".example.org" => "example.org"
                domains.Add(domain);
            }
#if DEBUG
            var sortedIPAddrs = from ipaddr in ipaddrs orderby ipaddr select ipaddr;
            var sortedDomains = from domain in domains orderby domain select domain;
            var result = sortedIPAddrs.Concat(sortedDomains);
            return result.ToArray();
#else
            var result = ipaddrs.Concat(domains);
            return result.ToArray();
#endif
        }

        /// <summary>
        /// Pick out the shortest domain name and drop the other longers from a string array,
        /// e.g. ("immigraion.gov.tw" and "gov.tw") => "gov.tw".
        /// </summary>
        /// <param name="domainNames">A string array of domain names.</param>
        /// <returns>A string array of domain names.</returns>
        private string[] CompactDomainNames(string[] domainNames)
        {
            var result = new HashSet<string>();
            foreach (var domain in domainNames)
            {
                var domainParts = domain.Split('.');
                var curDomainPart = domainParts[domainParts.Length - 1];
                for (int i = domainParts.Length - 2; i >= 0; i--)
                {
                    if (domainNames.Contains(curDomainPart))
                    {
                        Logging.Info($"Compacted domain: \"{domain}\" => \"{curDomainPart}\".");
                        break;
                    }
                    else
                        curDomainPart = domainParts[i] + "." + curDomainPart;
                }
                result.Add(curDomainPart);
            }
            return result.ToArray();
        }
    }
}
