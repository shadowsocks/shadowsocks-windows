using System;

namespace Shadowsocks.Model
{
    /*
     * Data come from WinINET
     */

    [Serializable]
    public class SysproxyConfig
    {
        public bool UserSettingsRecorded;
        public string Flags;
        public string ProxyServer;
        public string BypassList;
        public string PacUrl;

        public SysproxyConfig()
        {
            UserSettingsRecorded = false;
            Flags = "1";
            // Watchout, Nullable! See #2100
            ProxyServer = "";
            BypassList = "";
            PacUrl = "";
        }
    }
}