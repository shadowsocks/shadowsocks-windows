using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.Controller
{
    public class GfwListUpdater
    {
        private const string GFWLIST_URL = "https://autoproxy-gfwlist.googlecode.com/svn/trunk/gfwlist.txt";

        public IWebProxy proxy = null;

        public class GfwListDownloadCompletedArgs : EventArgs
        {
            public string Content;
        }

        public event EventHandler<GfwListDownloadCompletedArgs> DownloadCompleted;

        public event ErrorEventHandler Error;

        public void Download()
        {
            WebClient http = new WebClient();
            http.Proxy = proxy;
            http.DownloadStringCompleted += http_DownloadStringCompleted;
            http.DownloadStringAsync(new Uri(GFWLIST_URL));
        }

        protected void ReportError(Exception e)
        {
            if (Error != null)
            {
                Error(this, new ErrorEventArgs(e));
            }
        }

        private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                string response = e.Result;
                if (DownloadCompleted != null)
                {
                    DownloadCompleted(this, new GfwListDownloadCompletedArgs
                    {
                        Content = response
                    });
                }
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        public class Parser
        {
            private string _Content;

            public string Content
            {
                get { return _Content; }
            }

            public Parser(string response)
            {
                byte[] bytes = Convert.FromBase64String(response);
                this._Content = Encoding.ASCII.GetString(bytes);
            }

            public string[] GetValidLines()
            {
                string[] lines = Content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> valid_lines = new List<string>(lines.Length);
                foreach (string line in lines)
                {
                    if (line.StartsWith("!") || line.StartsWith("["))
                        continue;
                    valid_lines.Add(line);
                }
                return valid_lines.ToArray();
            }

            /* refer https://github.com/clowwindy/gfwlist2pac/blob/master/gfwlist2pac/main.py */
            public string[] GetDomains()
            {
                List<string> lines = new List<string>(GetValidLines());
                lines.AddRange(GetBuildIn());
                List<string> domains = new List<string>(lines.Count);
                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i];
                    if (line.IndexOf(".*") >= 0)
                        continue;
                    if (line.StartsWith("http://"))
                        line = line.Substring(7);
                    else if (line.StartsWith("https://"))
                        line = line.Substring(8);
                    if (line.IndexOf("*") >= 0)
                        line = line.Replace("*", "/");
                    if (line.StartsWith("||"))
                        while (line.StartsWith("||"))
                            line = line.Substring(2);
                    else if (line.StartsWith("|"))
                        line = line.TrimStart('|');
                    else if (line.StartsWith("."))
                        line = line.TrimStart('.');
                    if (line.StartsWith("!"))
                        continue;
                    else if (line.StartsWith("["))
                        continue;
                    else if (line.StartsWith("@"))
                        continue; /*ignore white list*/
                    int pos = line.IndexOfAny(new char[] { '/' });
                    if (pos >= 0)
                        line = line.Substring(0, pos);
                    if (line.Length > 0)
                        domains.Add(line);
                }
                return RemoveDuplicate(domains.ToArray());
            }

            /* refer https://github.com/clowwindy/gfwlist2pac/blob/master/gfwlist2pac/main.py */
            public string[] GetReducedDomains()
            {
                string[] domains = GetDomains();
                List<string> new_domains = new List<string>(domains.Length);
                TldIndex tldIndex = GetTldIndex();

                foreach (string domain in domains)
                {
                    string last_root_domain = null;
                    int pos;
                    pos = domain.LastIndexOf('.');
                    last_root_domain = domain.Substring(pos + 1);
                    if (!tldIndex.Contains(last_root_domain))
                        continue;
                    while (pos > 0)
                    {
                        pos = domain.LastIndexOf('.', pos - 1);
                        last_root_domain = domain.Substring(pos + 1);
                        if (tldIndex.Contains(last_root_domain))
                            continue;
                        else
                            break;
                    }
                    if (last_root_domain != null)
                        new_domains.Add(last_root_domain);
                }

                return RemoveDuplicate(new_domains.ToArray());
            }

            private string[] RemoveDuplicate(string[] src)
            {
                List<string> list = new List<string>(src.Length);
                Dictionary<string, string> dic = new Dictionary<string, string>(src.Length);
                foreach (string s in src)
                {
                    if (!dic.ContainsKey(s))
                    {
                        dic.Add(s, s);
                        list.Add(s);
                    }
                }
                return list.ToArray();
            }

            private string[] GetTlds()
            {
                string[] tlds = null;
                byte[] pacGZ = Resources.tld_txt;
                byte[] buffer = new byte[1024];
                int n;
                using (MemoryStream sb = new MemoryStream())
                {
                    using (GZipStream input = new GZipStream(new MemoryStream(pacGZ),
                        CompressionMode.Decompress, false))
                    {
                        while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            sb.Write(buffer, 0, n);
                        }
                    }
                    tlds = System.Text.Encoding.UTF8.GetString(sb.ToArray())
                        .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
                return tlds;
            }

            private TldIndex GetTldIndex()
            {
                string[] tlds = GetTlds();
                TldIndex index = new TldIndex();
                foreach (string tld in tlds)
                {
                    index.Add(tld);
                }
                return index;
            }

            private string[] GetBuildIn()
            {
                string[] buildin = null;
                byte[] builtinGZ = Resources.builtin_txt;
                byte[] buffer = new byte[1024];
                int n;
                using (MemoryStream sb = new MemoryStream())
                {
                    using (GZipStream input = new GZipStream(new MemoryStream(builtinGZ),
                        CompressionMode.Decompress, false))
                    {
                        while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            sb.Write(buffer, 0, n);
                        }
                    }
                    buildin = System.Text.Encoding.UTF8.GetString(sb.ToArray())
                        .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
                return buildin;
            }

            public class TldIndex
            {
                List<string> patterns = new List<string>();
                IDictionary<string, string> dic = new Dictionary<string, string>();

                public void Add(string tld)
                {
                    if (string.IsNullOrEmpty(tld))
                        return;
                    if (tld.IndexOfAny(new char[] { '*', '?' }) >= 0)
                    {
                        patterns.Add("^" + Regex.Escape(tld).Replace("\\*", ".*").Replace("\\?", ".") + "$");
                    }
                    else if (!dic.ContainsKey(tld))
                    {
                        dic.Add(tld, tld);
                    }
                }

                public bool Contains(string tld)
                {
                    if (dic.ContainsKey(tld))
                        return true;
                    foreach (string pattern in patterns)
                    {
                        if (Regex.IsMatch(tld, pattern))
                            return true;
                    }
                    return false;
                }

            }


        }

    }
}
