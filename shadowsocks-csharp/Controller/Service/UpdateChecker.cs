using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;
using NLog;
using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    public class UpdateChecker
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private const string UpdateURL = "https://api.github.com/repos/shadowsocks/shadowsocks-windows/releases";
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36";

        public bool NewVersionFound;
        public string LatestVersionNumber;
        public string LatestVersionSuffix;
        public string LatestVersionName;
        public string LatestVersionURL;
        public string LatestVersionLocalName;

        public const string Version = "4.1.9.2";

        public class Asset : IComparable<Asset>
        {
            public bool prerelease;
            public string name;
            public string version;
            public string browser_download_url;
            public string suffix;

            public static Asset ParseAsset(JObject assertJObject)
            {
                var name = (string)assertJObject["name"];
                Match match = Regex.Match(name, @"^Shadowsocks-(?<version>\d+(?:\.\d+)*)(?:|-(?<suffix>.+))\.\w+$",
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string version = match.Groups["version"].Value;

                    var asset = new Asset
                    {
                        browser_download_url = (string)assertJObject["browser_download_url"],
                        name = name,
                        version = version
                    };

                    if (match.Groups["suffix"].Success)
                    {
                        asset.suffix = match.Groups["suffix"].Value;
                    }

                    return asset;
                }

                return null;
            }

            public bool IsNewVersion(string currentVersion, bool checkPreRelease)
            {
                if (prerelease && !checkPreRelease)
                {
                    return false;
                }
                if (version == null)
                {
                    return false;
                }
                var cmp = CompareVersion(version, currentVersion);
                return cmp > 0;
            }

            public static int CompareVersion(string l, string r)
            {
                var ls = l.Split('.');
                var rs = r.Split('.');
                for (int i = 0; i < Math.Max(ls.Length, rs.Length); i++)
                {
                    int lp = (i < ls.Length) ? int.Parse(ls[i]) : 0;
                    int rp = (i < rs.Length) ? int.Parse(rs[i]) : 0;
                    if (lp != rp)
                    {
                        return lp - rp;
                    }
                }
                return 0;
            }

            public int CompareTo(Asset other)
            {
                return CompareVersion(version, other.version);
            }
        }
    }
}
