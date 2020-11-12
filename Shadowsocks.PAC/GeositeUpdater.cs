using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;

namespace Shadowsocks.PAC
{
    public class GeositeResultEventArgs : EventArgs
    {
        public bool Success;

        public GeositeResultEventArgs(bool success)
        {
            Success = success;
        }
    }

    public class GeositeUpdater : IEnableLogger
    {
        public event EventHandler<GeositeResultEventArgs>? UpdateCompleted;

        private readonly string DATABASE_PATH;

        private readonly string GEOSITE_URL = "https://github.com/v2fly/domain-list-community/raw/release/dlc.dat";
        private readonly string GEOSITE_SHA256SUM_URL = "https://github.com/v2fly/domain-list-community/raw/release/dlc.dat.sha256sum";
        private byte[] geositeDB;

        public readonly Dictionary<string, IList<DomainObject>> Geosites = new Dictionary<string, IList<DomainObject>>();

        public GeositeUpdater(string dlcPath)
        {
            DATABASE_PATH = dlcPath;
            if (File.Exists(DATABASE_PATH) && new FileInfo(DATABASE_PATH).Length > 0)
            {
                geositeDB = File.ReadAllBytes(DATABASE_PATH);
            }
            else
            {
                geositeDB = Properties.Resources.dlc;
                File.WriteAllBytes(DATABASE_PATH, Properties.Resources.dlc);
            }
            LoadGeositeList();
        }

        /// <summary>
        /// load new GeoSite data from geositeDB
        /// </summary>
        private void LoadGeositeList()
        {
            var list = GeositeList.Parser.ParseFrom(geositeDB);
            foreach (var item in list.Entries)
            {
                Geosites[item.GroupName.ToLowerInvariant()] = item.Domains;
            }
        }

        public void ResetEvent()
        {
            UpdateCompleted = null;
        }

        public async Task UpdatePACFromGeosite(PACSettings pACSettings)
        {
            string geositeUrl = GEOSITE_URL;
            string geositeSha256sumUrl = GEOSITE_SHA256SUM_URL;
            SHA256 mySHA256 = SHA256.Create();
            bool blacklist = pACSettings.PACDefaultToDirect;
            var httpClient = Locator.Current.GetService<HttpClient>();

            if (!string.IsNullOrWhiteSpace(pACSettings.CustomGeositeUrl))
            {
                this.Log().Info("Found custom Geosite URL in config file");
                geositeUrl = pACSettings.CustomGeositeUrl;
            }
            this.Log().Info($"Checking Geosite from {geositeUrl}");

            try
            {
                // download checksum first
                var geositeSha256sum = await httpClient.GetStringAsync(geositeSha256sumUrl);
                geositeSha256sum = geositeSha256sum.Substring(0, 64).ToUpper();
                this.Log().Info($"Got Sha256sum: {geositeSha256sum}");
                // compare downloaded checksum with local geositeDB
                byte[] localDBHashBytes = mySHA256.ComputeHash(geositeDB);
                string localDBHash = BitConverter.ToString(localDBHashBytes).Replace("-", String.Empty);
                this.Log().Info($"Local Sha256sum: {localDBHash}");
                // if already latest
                if (geositeSha256sum == localDBHash)
                {
                    this.Log().Info("Local GeoSite DB is up to date.");
                    return;
                }

                // not latest. download new DB
                var downloadedBytes = await httpClient.GetByteArrayAsync(geositeUrl);

                // verify sha256sum
                byte[] downloadedDBHashBytes = mySHA256.ComputeHash(downloadedBytes);
                string downloadedDBHash = BitConverter.ToString(downloadedDBHashBytes).Replace("-", String.Empty);
                this.Log().Info($"Actual Sha256sum: {downloadedDBHash}");
                if (geositeSha256sum != downloadedDBHash)
                {
                    this.Log().Info("Sha256sum Verification: FAILED. Downloaded GeoSite DB is corrupted. Aborting the update.");
                    throw new Exception("Sha256sum mismatch");
                }
                else
                {
                    this.Log().Info("Sha256sum Verification: PASSED. Applying to local GeoSite DB.");
                }

                // write to geosite file
                using (FileStream geositeFileStream = File.Create(DATABASE_PATH))
                    await geositeFileStream.WriteAsync(downloadedBytes, 0, downloadedBytes.Length);

                // update stuff
                geositeDB = downloadedBytes;
                LoadGeositeList();
                bool pacFileChanged = MergeAndWritePACFile(pACSettings.GeositeDirectGroups, pACSettings.GeositeProxiedGroups, blacklist);
                UpdateCompleted?.Invoke(null, new GeositeResultEventArgs(pacFileChanged));
            }
            catch (Exception e)
            {
                this.Log().Error(e, "An error occurred while updating PAC.");
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
        public bool MergeAndWritePACFile(List<string> directGroups, List<string> proxiedGroups, bool blacklist)
        {
            string abpContent = MergePACFile(directGroups, proxiedGroups, blacklist);
            if (File.Exists(PACDaemon.PAC_FILE))
            {
                string original = File.ReadAllText(PACDaemon.PAC_FILE);
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
        public bool CheckGeositeGroup(string group) => SeparateAttributeFromGroupName(group, out string groupName, out _) && Geosites.ContainsKey(groupName);

        /// <summary>
        /// Separates the attribute (e.g. @cn) from a group name.
        /// No checks are performed.
        /// </summary>
        /// <param name="group">A group name potentially with a trailing attribute.</param>
        /// <param name="groupName">The group name with the attribute stripped.</param>
        /// <param name="attribute">The attribute.</param>
        /// <returns>True for success. False for more than one '@'.</returns>
        private bool SeparateAttributeFromGroupName(string group, out string groupName, out string attribute)
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

        private string MergePACFile(List<string> directGroups, List<string> proxiedGroups, bool blacklist)
        {
            string abpContent;
            if (File.Exists(PACDaemon.USER_ABP_FILE))
            {
                abpContent = File.ReadAllText(PACDaemon.USER_ABP_FILE);
            }
            else
            {
                abpContent = Properties.Resources.abp;
            }

            List<string> userruleLines = new List<string>();
            if (File.Exists(PACDaemon.USER_RULE_FILE))
            {
                string userrulesString = File.ReadAllText(PACDaemon.USER_RULE_FILE);
                userruleLines = ProcessUserRules(userrulesString);
            }

            List<string> ruleLines = GenerateRules(directGroups, proxiedGroups, blacklist);

            var jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };
            abpContent =
$@"var __USERRULES__ = {JsonSerializer.Serialize(userruleLines, jsonSerializerOptions)};
var __RULES__ = {JsonSerializer.Serialize(ruleLines, jsonSerializerOptions)};
{abpContent}";
            return abpContent;
        }

        private List<string> ProcessUserRules(string content)
        {
            List<string> valid_lines = new List<string>();
            using (var stringReader = new StringReader(content))
            {
                for (string? line = stringReader.ReadLine(); line != null; line = stringReader.ReadLine())
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
        private List<string> GenerateRules(List<string> directGroups, List<string> proxiedGroups, bool blacklist)
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
        private List<string> GenerateBlockingRules(List<string> groups)
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
        private List<string> GenerateExceptionRules(List<string> groups)
            => GenerateBlockingRules(groups)
                .Select(r => $"@@{r}") // convert blocking rules to exception rules
                .ToList();
    }
}
