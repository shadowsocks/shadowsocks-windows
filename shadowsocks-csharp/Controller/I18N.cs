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
            // https://msdn.microsoft.com/en-us/library/system.globalization.cultureinfo.threeletterwindowslanguagename(v=vs.110).aspx

            // CULTURE ISO ISO WIN ENGLISHNAME
            // zh      zh  zho CHS Chinese
            // zh-Hans zh  zho CHS Chinese (Simplified)
            // zh-Hant zh  zho CHT Chinese(Traditional)
            // zh-CHS  zh  zho CHS Chinese(Simplified) Legacy
            // zh-CHT  zh  zho CHT Chinese(Traditional) Legacy
            // ja      ja  jpn JPN Japanese
            // en      en  eng ENU English

            switch (CultureInfo.CurrentCulture.ThreeLetterWindowsLanguageName)
            {
                case "CHS": Init(Resources.CHS); break;
                case "CHT": Init(Resources.CHT); break;
                case "JPN": Init(Resources.JPN); break;
                default: break; // default is English
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
