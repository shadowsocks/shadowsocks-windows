using System;

namespace Shadowsocks.Model
{
    [Serializable]
    public class HotkeyConfig
    {
        public string SwitchSystemProxy;
        public string ChangeToPac;
        public string ChangeToGlobal;
        public string SwitchAllowLan;
        public string ShowLogs;
        public bool AllowSwitchServer = false;
    }
}