using Shadowsocks.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Shadowsocks.Controller
{
    public class UpdateChecker
    {
        //private const string UpdateURL = "https://sourceforge.net/api/file/index/project-id/1817190/path/dist/mtime/desc/limit/10/rss";
        private const string UpdateURL = "https://raw.github.com/breakwa11/shadowsocks-rss/master/shadowsocks-win.xml";

        public string LatestVersionNumber;
        public string LatestVersionURL;
        public event EventHandler NewVersionFound;

        public const string Name = "ShadowsocksR";
        public const string Copyright = "Copyright © BreakWall 2015";
        public const string Version = "3.7.3";
        public const string FullVersion = Version + "";

        private static bool UseProxy = true;

        public void CheckUpdate(Configuration config)
        {
            try
            {
                WebClient http = new WebClient();
                http.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36");
                if (UseProxy)
                {
                    http.Proxy = new WebProxy(IPAddress.Loopback.ToString(), config.localPort);
                }
                else
                {
                    http.Proxy = null;
                }
                UseProxy = !UseProxy;
                http.DownloadStringCompleted += http_DownloadStringCompleted;
                http.DownloadStringAsync(new Uri(UpdateURL));
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
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

        public class VersionComparer : IComparer<string>
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed. 
            public int Compare(string x, string y)
            {
                return CompareVersion(ParseVersionFromURL(x), ParseVersionFromURL(y));
            }

        }

        private static string ParseVersionFromURL(string url)
        {
            Match match = Regex.Match(url, @".*" + Name + @"-win.*?-([\d\.]+)\.\w+", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (match.Groups.Count == 2)
                {
                    return match.Groups[1].Value;
                }
            }
            return null;
        }

        private void SortVersions(List<string> versions)
        {
            versions.Sort(new VersionComparer());
        }

        private bool IsNewVersion(string url)
        {
            if (url.IndexOf("prerelease") >= 0)
            {
                return false;
            }
            // check dotnet 4.0
            AssemblyName[] references = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            Version dotNetVersion = Environment.Version;
            foreach (AssemblyName reference in references)
            {
                if (reference.Name == "mscorlib")
                {
                    dotNetVersion = reference.Version;
                }
            }
            if (dotNetVersion.Major >= 4)
            {
                if (url.IndexOf("dotnet4.0") < 0)
                {
                    return false;
                }
            }
            else
            {
                if (url.IndexOf("dotnet4.0") >= 0)
                {
                    return false;
                }
            }
            string version = ParseVersionFromURL(url);
            if (version == null)
            {
                return false;
            }
            string currentVersion = Version;

            return CompareVersion(version, currentVersion) > 0;
        }

        private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                string response = e.Result;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                XmlNodeList elements = xmlDoc.GetElementsByTagName("media:content");
                List<string> versions = new List<string>();
                foreach (XmlNode el in elements)
                {
                    foreach (XmlAttribute attr in el.Attributes)
                    {
                        if (attr.Name == "url")
                        {
                            if (IsNewVersion(attr.Value))
                            {
                                versions.Add(attr.Value);
                            }
                        }
                    }
                }
                if (versions.Count == 0)
                {
                    return;
                }
                // sort versions
                SortVersions(versions);
                LatestVersionURL = versions[versions.Count - 1];
                LatestVersionNumber = ParseVersionFromURL(LatestVersionURL);
                if (NewVersionFound != null)
                {
                    NewVersionFound(this, new EventArgs());
                }
            }
            catch (Exception ex)
            {
                Logging.Debug(ex.ToString());
                if (NewVersionFound != null)
                {
                    NewVersionFound(this, new EventArgs());
                }
                return;
            }
        }
    }
}
