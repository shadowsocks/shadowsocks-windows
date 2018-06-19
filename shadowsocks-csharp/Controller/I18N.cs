using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Shadowsocks.Controller
{
    using Shadowsocks.Properties;

    public static class I18N
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();

        private static void Init(string res)
        {
            using (var sr = new StringReader(res))
            {
                foreach (var line in sr.NonWhiteSpaceLines())
                {
                    if (line[0] == '#')
                        continue;

                    var pos = line.IndexOf('=');
                    if (pos < 1)
                        continue;
                    _strings[line.Substring(0, pos)] = line.Substring(pos + 1);
                }
            }
        }

        static I18N()
        {
            string name = CultureInfo.CurrentCulture.EnglishName;
            if (name.StartsWith("Chinese", StringComparison.OrdinalIgnoreCase))
            {
                // choose Traditional Chinese only if we get explicit indication
                Init(name.Contains("Traditional")
                    ? Resources.zh_TW
                    : Resources.zh_CN);
            }
            else if (name.StartsWith("Japan", StringComparison.OrdinalIgnoreCase))
            {
                Init(Resources.ja);
            }
        }

        public static string GetString(string key)
        {
            return _strings.ContainsKey(key)
                ? _strings[key]
                : key;
        }
    }
}
