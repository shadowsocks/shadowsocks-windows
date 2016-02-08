using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Shadowsocks.Controller
{
    using Shadowsocks.Properties;

    public class I18N
    {
        protected static Dictionary<string, string> Strings;
        static I18N()
        {
            Strings = new Dictionary<string, string>();

            if (CultureInfo.CurrentCulture.IetfLanguageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                using (var sr = new StringReader(Resources.cn))
                {
                    foreach (var line in sr.NonWhiteSpaceLines())
                    {
                        if (line[0] == '#')
                            continue;

                        var pos = line.IndexOf('=');
                        if (pos < 1)
                            continue;
                        Strings[line.Substring(0, pos)] = line.Substring(pos + 1);
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
