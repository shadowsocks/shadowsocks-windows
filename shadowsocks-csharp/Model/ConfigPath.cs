using System;
using System.Collections.Generic;
using System.IO;

namespace Shadowsocks.Model
{
    class ConfigPath
    {
        const string PATH_FILE = "path";

        public static string Load()
        {
            try
            {
                string path = File.ReadAllText(PATH_FILE);
                DirectoryInfo di;
                if (Directory.Exists(path))
                {
                    di = new DirectoryInfo(path);
                }
                else
                {
                    di = Directory.CreateDirectory(path);
                }
                return di.FullName + "\\";
            }
            catch
            {
                return "";
            }
        }
    }
}
