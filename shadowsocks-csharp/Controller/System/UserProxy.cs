using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Util;
using Shadowsocks.Util.SystemProxy;

namespace Shadowsocks.Controller
{
    /// <summary>
    /// Record and restore user's proxy settings.
    /// </summary>
    public static class UserProxy
    {
        private static readonly Dictionary<string, string> ProxyValues = new Dictionary<string, string>();

        public static void Record()
        {
            try
            {
                // query returns:
                //flags=1
                //proxy-server=(null)
                //bypass-list=(null)
                //pac-url=(null)
                var query = Sysproxy.SetIEProxyWithArguments("query");
                if (query.IsNullOrWhiteSpace())
                {
                    return;
                }

                using (var sr = new StringReader(query))
                {
                    foreach (var line in sr.NonWhiteSpaceLines())
                    {
                        if (line.Contains("(null)"))
                        {
                            continue;
                        }

                        var pos = line.IndexOf('=');
                        ProxyValues[line.Substring(0, pos)] = line.Substring(pos + 1);
                    }
                }
            }
            catch (ProxyException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public static void Restore()
        {
            if (ProxyValues == null)
            {
                return;
            }

            try
            {
                // if flags is null, set proxy to auto detect.
                var arguments = $"set {(ProxyValues.ContainsKey("flags") ? ProxyValues["flags"] : "9")} {GetValue("proxy-server")} {GetValue("bypass-list")} {GetValue("pac-url")}";
                Sysproxy.SetIEProxyWithArguments(arguments);
            }
            catch (ProxyException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private static string GetValue(string key)
        {
            ProxyValues.TryGetValue(key, out string value);
            return value ?? "-";
        }
    }
}