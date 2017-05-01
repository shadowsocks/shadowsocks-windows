using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlobalHotKey;
using Shadowsocks.Controller.Hotkeys;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    static class StartupHotkeyReg
    {
        public static Dictionary<HotKey, HotKeys.HotKeyCallBackHandler> hotKeyDic;
        public static void Init()
        {
            if (Configuration.Load().hotkey == null || !Configuration.Load().hotkey.RegAllAtStartup)
                return;
            try
            {
                InitDic();
                Reg();
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }
        }
        private static void InitDic()
        {
            hotKeyDic = new Dictionary<HotKey, HotKeys.HotKeyCallBackHandler>();
            var _hotKeyConf = Configuration.Load().hotkey;

            if (!_hotKeyConf.SwitchSystemProxy.IsNullOrEmpty())
            {
                hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.SwitchSystemProxy)
                    , HotkeyCallbacks.GetCallback("SwitchSystemProxyCallback") as HotKeys.HotKeyCallBackHandler);
            }

            if (!_hotKeyConf.SwitchSystemProxyMode.IsNullOrEmpty())
            {
                hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.SwitchSystemProxyMode)
                    , HotkeyCallbacks.GetCallback("SwitchProxyModeCallback") as HotKeys.HotKeyCallBackHandler);
            }

            if (!_hotKeyConf.SwitchAllowLan.IsNullOrEmpty())
            {
                hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.SwitchAllowLan)
                    , HotkeyCallbacks.GetCallback("SwitchAllowLanCallback") as HotKeys.HotKeyCallBackHandler);
            }

            if (!_hotKeyConf.ShowLogs.IsNullOrEmpty())
            {
                hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.ShowLogs)
                    , HotkeyCallbacks.GetCallback("ShowLogsCallback") as HotKeys.HotKeyCallBackHandler);
            }

            if (!_hotKeyConf.ServerMoveUp.IsNullOrEmpty())
            {
                hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.ServerMoveUp)
                    , HotkeyCallbacks.GetCallback("ServerMoveUpCallback") as HotKeys.HotKeyCallBackHandler);
            }

            if (!_hotKeyConf.ServerMoveDown.IsNullOrEmpty())
            {
                hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.ServerMoveDown)
                    , HotkeyCallbacks.GetCallback("ServerMoveDownCallback") as HotKeys.HotKeyCallBackHandler);
            }
        }

        private static void Reg()
        {
            foreach (var v in hotKeyDic)
            {
                HotKeys.Regist(v.Key, v.Value);
            }
        }
    }
}
