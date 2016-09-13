using System;

namespace Shadowsocks.Model
{
    /*
     * Key codes are stored as enum, and they actually
     * use type: int
     *
     * Format:
     *  <keycode-number>|<key-modifiers-combination-number>
     *
     */
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