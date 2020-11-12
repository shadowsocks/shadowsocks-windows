using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.WPF.Models
{
    public class AppSettings
    {
        public bool StartOnBoot { get; set; }
        public bool AssociateSsLinks { get; set; }
        public bool VersionUpdateCheckForPrerelease { get; set; }
        public string SkippedUpdateVersion { get; set; }

        public AppSettings()
        {
            StartOnBoot = false;
            AssociateSsLinks = false;
            VersionUpdateCheckForPrerelease = false;
            SkippedUpdateVersion = "";
        }
    }
}
