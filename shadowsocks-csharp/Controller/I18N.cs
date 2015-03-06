using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Shadowsocks.Controller
{
    public class I18N
    {
        protected static Dictionary<string, string> Strings;
        static I18N()
        {
            Strings = new Dictionary<string, string>();
            
            if (System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag.ToLowerInvariant().StartsWith("zh"))
            {
                string[] lines = Regex.Split(Resources.cn, "\r\n|\r|\n");
                foreach (string line in lines)
                {
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }
                    string[] kv = Regex.Split(line, "=");
                    if (kv.Length == 2)
                    {
                        Strings[kv[0]] = kv[1];
                    }
                }
            }
        }

        public static string GetString(string key)
        {
            if (Strings.ContainsKey(key))
            {
                return Strings[key];
            }
            else
            {
                return key;
            }
        }
    }
}
