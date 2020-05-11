using NLog;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Shadowsocks.Model;
using System.Net;

namespace Shadowsocks.Controller
{
    public class GeositeResultEventArgs : EventArgs
    {
        public bool Success;

        public GeositeResultEventArgs(bool success)
        {
            this.Success = success;
        }
    }

    public static class GeositeUpdater
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static event EventHandler<GeositeResultEventArgs> UpdateCompleted;

        public static event ErrorEventHandler Error;

        private static readonly string DATABASE_PATH = Utils.GetTempPath("dlc.dat");

        private static readonly string GEOSITE_URL = "https://github.com/v2ray/domain-list-community/raw/release/dlc.dat";

        public static readonly Dictionary<string, IList<DomainObject>> Geosites = new Dictionary<string, IList<DomainObject>>();

        static GeositeUpdater()
        {
            if (!File.Exists(DATABASE_PATH))
            {
                File.WriteAllBytes(DATABASE_PATH, Resources.dlc_dat);
            }
            LoadGeositeList();
        }

        static void LoadGeositeList(byte[] data = null)
        {
            data = data ?? File.ReadAllBytes(DATABASE_PATH);
            var list = GeositeList.Parser.ParseFrom(data);
            foreach (var item in list.Entries)
            {
                Geosites[item.GroupName.ToLower()] = item.Domains;
            }
        }

        public static void ResetEvent()
        {
            UpdateCompleted = null;
            Error = null;
        }

        public static void UpdatePACFromGeosite(Configuration config)
        {
            string geositeUrl = GEOSITE_URL;
            string group = config.geositeGroup;
            bool blacklist = config.geositeBlacklistMode;
            
            if (!string.IsNullOrWhiteSpace(config.geositeUrl))
            {
                logger.Info("Found custom Geosite URL in config file");
                geositeUrl = config.geositeUrl;
            }
            logger.Info($"Checking Geosite from {geositeUrl}");
            WebClient http = new WebClient();
            if (config.enabled)
            {
                http.Proxy = new WebProxy(
                    config.isIPv6Enabled
                    ? $"[{IPAddress.IPv6Loopback}]"
                    : IPAddress.Loopback.ToString(),
                    config.localPort);
            }
            http.DownloadDataCompleted += (o, e) =>
            {
                try
                {
                    File.WriteAllBytes(DATABASE_PATH, e.Result);
                    LoadGeositeList();

                    bool pacFileChanged = MergeAndWritePACFile(group, blacklist);
                    UpdateCompleted?.Invoke(null, new GeositeResultEventArgs(pacFileChanged));
                }
                catch (Exception ex)
                {
                    Error?.Invoke(null, new ErrorEventArgs(ex));
                }
            };
            http.DownloadDataAsync(new Uri(geositeUrl));
        }

        public static bool MergeAndWritePACFile(string group, bool blacklist)
        {
            IList<DomainObject> domains = Geosites[group];
            string abpContent = MergePACFile(domains, blacklist);
            if (File.Exists(PACDaemon.PAC_FILE))
            {
                string original = FileManager.NonExclusiveReadAllText(PACDaemon.PAC_FILE, Encoding.UTF8);
                if (original == abpContent)
                {
                    return false;
                }
            }
            File.WriteAllText(PACDaemon.PAC_FILE, abpContent, Encoding.UTF8);
            return true;
        }

        private static string MergePACFile(IList<DomainObject> domains, bool blacklist)
        {
            string abpContent;
            if (File.Exists(PACDaemon.USER_ABP_FILE))
            {
                abpContent = FileManager.NonExclusiveReadAllText(PACDaemon.USER_ABP_FILE, Encoding.UTF8);
            }
            else
            {
                abpContent = Resources.abp_js;
            }

            List<string> userruleLines = new List<string>();
            if (File.Exists(PACDaemon.USER_RULE_FILE))
            {
                string userrulesString = FileManager.NonExclusiveReadAllText(PACDaemon.USER_RULE_FILE, Encoding.UTF8);
                userruleLines = PreProcessGFWList(userrulesString);
            }

            List<string> gfwLines = GeositeToGFWList(domains, blacklist);
            abpContent =
$@"var __USERRULES__ = {JsonConvert.SerializeObject(userruleLines, Formatting.Indented)};
var __RULES__ = {JsonConvert.SerializeObject(gfwLines, Formatting.Indented)};
{abpContent}";
            return abpContent;
        }

        private static readonly IEnumerable<char> IgnoredLineBegins = new[] { '!', '[' };

        private static List<string> PreProcessGFWList(string content)
        {
            List<string> valid_lines = new List<string>();
            using (var sr = new StringReader(content))
            {
                foreach (var line in sr.NonWhiteSpaceLines())
                {
                    if (line.BeginWithAny(IgnoredLineBegins))
                        continue;
                    valid_lines.Add(line);
                }
            }
            return valid_lines;
        }

        private static List<string> GeositeToGFWList(IList<DomainObject> domains, bool blacklist)
        {
            return blacklist ? GeositeToGFWListBlack(domains) : GeositeToGFWListWhite(domains);
        }

        private static List<string> GeositeToGFWListBlack(IList<DomainObject> domains)
        {
            List<string> ret = new List<string>(domains.Count + 100);// 100 overhead
            foreach (var d in domains)
            {
                string domain = d.Value;

                switch (d.Type)
                {
                    case DomainObject.Types.Type.Plain:
                        ret.Add(domain);
                        break;
                    case DomainObject.Types.Type.Regex:
                        ret.Add($"/{domain}/");
                        break;
                    case DomainObject.Types.Type.Domain:
                        ret.Add($"||{domain}");
                        break;
                    case DomainObject.Types.Type.Full:
                        ret.Add($"|http://{domain}");
                        ret.Add($"|https://{domain}");
                        break;
                }
            }
            return ret;
        }

        private static List<string> GeositeToGFWListWhite(IList<DomainObject> domains)
        {
            return GeositeToGFWListBlack(domains)
                .Select(r => $"@@{r}") // convert to whitelist
                .Prepend("/.*/") // blacklist all other site
                .ToList();
        }
    }
}
