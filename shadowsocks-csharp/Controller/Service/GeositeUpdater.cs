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
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;

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

        private static HttpClientHandler httpClientHandler;
        private static HttpClient httpClient;
        private static readonly string GEOSITE_URL = "https://github.com/v2fly/domain-list-community/raw/release/dlc.dat";
        private static readonly string GEOSITE_SHA256SUM_URL = "https://github.com/v2fly/domain-list-community/raw/release/dlc.dat.sha256sum";
        private static byte[] geositeDB;

        public static readonly Dictionary<string, IList<DomainObject>> Geosites = new Dictionary<string, IList<DomainObject>>();

        static GeositeUpdater()
        {
            if (File.Exists(DATABASE_PATH) && new FileInfo(DATABASE_PATH).Length > 0)
            {
                geositeDB = File.ReadAllBytes(DATABASE_PATH);
            }
            else
            {
                geositeDB = Resources.dlc_dat;
                File.WriteAllBytes(DATABASE_PATH, Resources.dlc_dat);
            }
            LoadGeositeList();
        }

        /// <summary>
        /// load new GeoSite data from geositeDB
        /// </summary>
        static void LoadGeositeList()
        {
            var list = GeositeList.Parser.ParseFrom(geositeDB);
            foreach (var item in list.Entries)
            {
                Geosites[item.GroupName.ToLowerInvariant()] = item.Domains;
            }
        }

        public static void ResetEvent()
        {
            UpdateCompleted = null;
            Error = null;
        }

        public static async Task UpdatePACFromGeosite()
        {
            string geositeUrl = GEOSITE_URL;
            string geositeSha256sumUrl = GEOSITE_SHA256SUM_URL;
            SHA256 mySHA256 = SHA256.Create();
            var config = Program.MainController.GetCurrentConfiguration();
            bool blacklist = config.geositePreferDirect;
            
            if (!string.IsNullOrWhiteSpace(config.geositeUrl))
            {
                logger.Info("Found custom Geosite URL in config file");
                geositeUrl = config.geositeUrl;
            }
            logger.Info($"Checking Geosite from {geositeUrl}");

            // use System.Net.Http.HttpClient to download GeoSite db.
            // NASTY workaround: new HttpClient every update
            // because we can't change proxy on existing socketsHttpHandler instance
            httpClientHandler = new HttpClientHandler();
            httpClient = new HttpClient(httpClientHandler);
            if (!string.IsNullOrWhiteSpace(config.userAgentString))
                httpClient.DefaultRequestHeaders.Add("User-Agent", config.userAgentString);
            if (config.enabled)
            {
                httpClientHandler.Proxy = new WebProxy(
                    config.isIPv6Enabled
                    ? $"[{IPAddress.IPv6Loopback}]"
                    : IPAddress.Loopback.ToString(),
                    config.localPort);
            }

            try
            {
                // download checksum first
                var geositeSha256sum = await httpClient.GetStringAsync(geositeSha256sumUrl);
                geositeSha256sum = geositeSha256sum.Substring(0, 64).ToUpper();
                logger.Info($"Got Sha256sum: {geositeSha256sum}");
                // compare downloaded checksum with local geositeDB
                byte[] localDBHashBytes = mySHA256.ComputeHash(geositeDB);
                string localDBHash = BitConverter.ToString(localDBHashBytes).Replace("-", String.Empty);
                logger.Info($"Local Sha256sum: {localDBHash}");
                // if already latest
                if (geositeSha256sum == localDBHash)
                {
                    logger.Info("Local GeoSite DB is up to date.");
                    return;
                }

                // not latest. download new DB
                var downloadedBytes = await httpClient.GetByteArrayAsync(geositeUrl);

                // verify sha256sum
                byte[] downloadedDBHashBytes = mySHA256.ComputeHash(downloadedBytes);
                string downloadedDBHash = BitConverter.ToString(downloadedDBHashBytes).Replace("-", String.Empty);
                logger.Info($"Actual Sha256sum: {downloadedDBHash}");
                if (geositeSha256sum != downloadedDBHash)
                {
                    logger.Info("Sha256sum Verification: FAILED. Downloaded GeoSite DB is corrupted. Aborting the update.");
                    throw new Exception("Sha256sum mismatch");
                }
                else
                {
                    logger.Info("Sha256sum Verification: PASSED. Applying to local GeoSite DB.");
                }

                // write to geosite file
                using (FileStream geositeFileStream = File.Create(DATABASE_PATH))
                    await geositeFileStream.WriteAsync(downloadedBytes, 0, downloadedBytes.Length);

                // update stuff
                geositeDB = downloadedBytes;
                LoadGeositeList();
                bool pacFileChanged = MergeAndWritePACFile(config.geositeDirectGroups, config.geositeProxiedGroups, blacklist);
                UpdateCompleted?.Invoke(null, new GeositeResultEventArgs(pacFileChanged));
            }
            catch (Exception ex)
            {
                Error?.Invoke(null, new ErrorEventArgs(ex));
            }
            finally
            {
                if (httpClientHandler != null)
                {
                    httpClientHandler.Dispose();
                    httpClientHandler = null;
                }
                if (httpClient != null)
                {
                    httpClient.Dispose();
                    httpClient = null;
                }
            }
        }

        /// <summary>
        /// Merge and write pac.txt from geosite.
        /// Used at multiple places.
        /// </summary>
        /// <param name="directGroups">A list of geosite groups configured for direct connection.</param>
        /// <param name="proxiedGroups">A list of geosite groups configured for proxied connection.</param>
        /// <param name="blacklist">Whether to use blacklist mode. False for whitelist.</param>
        /// <returns></returns>
        public static bool MergeAndWritePACFile(List<string> directGroups, List<string> proxiedGroups, bool blacklist)
        {
            string abpContent = MergePACFile(directGroups, proxiedGroups, blacklist);
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

        /// <summary>
        /// Checks if the specified group exists in GeoSite database.
        /// </summary>
        /// <param name="group">The group name to check for.</param>
        /// <returns>True if the group exists. False if the group doesn't exist.</returns>
        public static bool CheckGeositeGroup(string group) => SeparateAttributeFromGroupName(group, out string groupName, out _) && Geosites.ContainsKey(groupName);

        /// <summary>
        /// Separates the attribute (e.g. @cn) from a group name.
        /// No checks are performed.
        /// </summary>
        /// <param name="group">A group name potentially with a trailing attribute.</param>
        /// <param name="groupName">The group name with the attribute stripped.</param>
        /// <param name="attribute">The attribute.</param>
        /// <returns>True for success. False for more than one '@'.</returns>
        private static bool SeparateAttributeFromGroupName(string group, out string groupName, out string attribute)
        {
            var splitGroupAttributeList = group.Split('@');
            if (splitGroupAttributeList.Length == 1) // no attribute
            {
                groupName = splitGroupAttributeList[0];
                attribute = "";
            }
            else if (splitGroupAttributeList.Length == 2) // has attribute
            {
                groupName = splitGroupAttributeList[0];
                attribute = splitGroupAttributeList[1];
            }
            else
            {
                groupName = "";
                attribute = "";
                return false;
            }    
            return true;
        }

        private static string MergePACFile(List<string> directGroups, List<string> proxiedGroups, bool blacklist)
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
                userruleLines = ProcessUserRules(userrulesString);
            }

            List<string> ruleLines = GenerateRules(directGroups, proxiedGroups, blacklist);
            abpContent =
$@"var __USERRULES__ = {JsonConvert.SerializeObject(userruleLines, Formatting.Indented)};
var __RULES__ = {JsonConvert.SerializeObject(ruleLines, Formatting.Indented)};
{abpContent}";
            return abpContent;
        }

        private static List<string> ProcessUserRules(string content)
        {
            List<string> valid_lines = new List<string>();
            using (var stringReader = new StringReader(content))
            {
                for (string line = stringReader.ReadLine(); line != null; line = stringReader.ReadLine())
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("!") || line.StartsWith("["))
                        continue;
                    valid_lines.Add(line);
                }
            }
            return valid_lines;
        }

        /// <summary>
        /// Generates rule lines based on user preference.
        /// </summary>
        /// <param name="directGroups">A list of geosite groups configured for direct connection.</param>
        /// <param name="proxiedGroups">A list of geosite groups configured for proxied connection.</param>
        /// <param name="blacklist">Whether to use blacklist mode. False for whitelist.</param>
        /// <returns>A list of rule lines.</returns>
        private static List<string> GenerateRules(List<string> directGroups, List<string> proxiedGroups, bool blacklist)
        {
            List<string> ruleLines;
            if (blacklist) // blocking + exception rules
            {
                ruleLines = GenerateBlockingRules(proxiedGroups);
                ruleLines.AddRange(GenerateExceptionRules(directGroups));
            }
            else // proxy all + exception rules
            {
                ruleLines = new List<string>()
                {
                    "/.*/" // block/proxy all unmatched domains
                };
                ruleLines.AddRange(GenerateExceptionRules(directGroups));
            }
            return ruleLines;
        }

        /// <summary>
        /// Generates rules that match domains that should be proxied.
        /// </summary>
        /// <param name="groups">A list of source groups.</param>
        /// <returns>A list of rule lines.</returns>
        private static List<string> GenerateBlockingRules(List<string> groups)
        {
            List<string> ruleLines = new List<string>();
            foreach (var group in groups)
            {
                // separate group name and attribute
                SeparateAttributeFromGroupName(group, out string groupName, out string attribute);
                var domainObjects = Geosites[groupName];
                if (!string.IsNullOrEmpty(attribute)) // has attribute
                {
                    var attributeObject = new DomainObject.Types.Attribute
                    {
                        Key = attribute,
                        BoolValue = true
                    };
                    foreach (var domainObject in domainObjects)
                    {
                        if (domainObject.Attribute.Contains(attributeObject))
                            switch (domainObject.Type)
                            {
                                case DomainObject.Types.Type.Plain:
                                    ruleLines.Add(domainObject.Value);
                                    break;
                                case DomainObject.Types.Type.Regex:
                                    ruleLines.Add($"/{domainObject.Value}/");
                                    break;
                                case DomainObject.Types.Type.Domain:
                                    ruleLines.Add($"||{domainObject.Value}");
                                    break;
                                case DomainObject.Types.Type.Full:
                                    ruleLines.Add($"|http://{domainObject.Value}");
                                    ruleLines.Add($"|https://{domainObject.Value}");
                                    break;
                            }
                    }
                }
                else // no attribute
                    foreach (var domainObject in domainObjects)
                    {
                        switch (domainObject.Type)
                        {
                            case DomainObject.Types.Type.Plain:
                                ruleLines.Add(domainObject.Value);
                                break;
                            case DomainObject.Types.Type.Regex:
                                ruleLines.Add($"/{domainObject.Value}/");
                                break;
                            case DomainObject.Types.Type.Domain:
                                ruleLines.Add($"||{domainObject.Value}");
                                break;
                            case DomainObject.Types.Type.Full:
                                ruleLines.Add($"|http://{domainObject.Value}");
                                ruleLines.Add($"|https://{domainObject.Value}");
                                break;
                        }
                    }
            }
            return ruleLines;
        }

        /// <summary>
        /// Generates rules that match domains that should be connected directly without a proxy.
        /// </summary>
        /// <param name="groups">A list of source groups.</param>
        /// <returns>A list of rule lines.</returns>
        private static List<string> GenerateExceptionRules(List<string> groups)
            => GenerateBlockingRules(groups)
                .Select(r => $"@@{r}") // convert blocking rules to exception rules
                .ToList();
    }
}
