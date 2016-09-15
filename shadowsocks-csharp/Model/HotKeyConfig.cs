using System;

namespace Shadowsocks.Model
{
    /*
     * Format:
     *  <modifiers-combination>+<key>
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
        public string ServerMoveUp;
        public string ServerMoveDown;

        public HotkeyConfig()
        {
            SwitchSystemProxy = "";
            ChangeToPac = "";
            ChangeToGlobal = "";
            SwitchAllowLan = "";
            ShowLogs = "";
            ServerMoveUp = "";
            ServerMoveDown = "";
        }
    }
}