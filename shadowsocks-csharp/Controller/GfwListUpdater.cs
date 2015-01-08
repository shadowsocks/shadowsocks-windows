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

        }

    }
}
