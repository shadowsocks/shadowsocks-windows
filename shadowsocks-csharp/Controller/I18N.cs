using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
namespace Shadowsocks.Controller
{

    public static class I18N
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();

        private static void Init(string res, string locale)
        {
            using (TextFieldParser csvParser = new TextFieldParser(new StringReader(res)))
            {
                csvParser.SetDelimiters(",");

                string[] localeNames = csvParser.ReadFields();

                int enIndex = 0;
                int targetIndex = 0;

                for (int i = 1; i < localeNames.Length; i++)
                {
                    if (localeNames[i] == "en")
                    {

                    }

                    if (localeNames[i] == locale)
                    {
                        targetIndex = i;
                    }
                }


                while (!csvParser.EndOfData)
                {
                    string[] translations = csvParser.ReadFields();
                    if (string.IsNullOrWhiteSpace(translations[0])) continue;
                    if (translations[0].TrimStart(' ')[0] == '#') continue;
                    _strings[translations[enIndex]] = translations[targetIndex];
                }
            }
        }

        static I18N()
        {
            Init(Resources.i18n_csv, CultureInfo.CurrentCulture.IetfLanguageTag);
        }

        public static string GetString(string key, params object[] args)
        {
            return string.Format(_strings.TryGetValue(key, out var value) ? value : key, args);
        }

        public static void TranslateForm(Form c)
        {
            c.Text = GetString(c.Text);
            foreach (var item in ViewUtils.GetChildControls<Control>(c))
            {
                item.Text = GetString(item.Text);
            }
            TranslateMenu(c.Menu);
        }
        public static void TranslateMenu(Menu m)
        {
            foreach (var item in ViewUtils.GetMenuItems(m))
            {
                item.Text = GetString(item.Text);
            }
        }
    }
}
