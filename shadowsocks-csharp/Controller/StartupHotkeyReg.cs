using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GlobalHotKey;
using Shadowsocks.Controller.Hotkeys;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    static class StartupHotkeyReg
    {
        public static void RegHotkey()
        {
            var _hotKeyConf = Configuration.Load().hotkey;

            if (_hotKeyConf == null || !_hotKeyConf.RegAllAtStartup)
                return;

            var _hotKeyDic = new Dictionary<HotKey, HotKeys.HotKeyCallBackHandler>();

            try
            {
                if (!_hotKeyConf.SwitchSystemProxy.IsNullOrEmpty())
                {
                    _hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.SwitchSystemProxy)
                        , HotkeyCallbacks.GetCallback("SwitchSystemProxyCallback") as HotKeys.HotKeyCallBackHandler);
                }

                if (!_hotKeyConf.SwitchSystemProxyMode.IsNullOrEmpty())
                {
                    _hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.SwitchSystemProxyMode)
                        , HotkeyCallbacks.GetCallback("SwitchProxyModeCallback") as HotKeys.HotKeyCallBackHandler);
                }

                if (!_hotKeyConf.SwitchAllowLan.IsNullOrEmpty())
                {
                    _hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.SwitchAllowLan)
                        , HotkeyCallbacks.GetCallback("SwitchAllowLanCallback") as HotKeys.HotKeyCallBackHandler);
                }

                if (!_hotKeyConf.ShowLogs.IsNullOrEmpty())
                {
                    _hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.ShowLogs)
                        , HotkeyCallbacks.GetCallback("ShowLogsCallback") as HotKeys.HotKeyCallBackHandler);
                }

                if (!_hotKeyConf.ServerMoveUp.IsNullOrEmpty())
                {
                    _hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.ServerMoveUp)
                        , HotkeyCallbacks.GetCallback("ServerMoveUpCallback") as HotKeys.HotKeyCallBackHandler);
                }

                if (!_hotKeyConf.ServerMoveDown.IsNullOrEmpty())
                {
                    _hotKeyDic.Add(HotKeys.Str2HotKey(_hotKeyConf.ServerMoveDown)
                        , HotkeyCallbacks.GetCallback("ServerMoveDownCallback") as HotKeys.HotKeyCallBackHandler);
                }

                foreach (var v in _hotKeyDic)
                {
                    if (!HotKeys.Regist(v.Key, v.Value))
                    {
                        MessageBox.Show(I18N.GetString("Register hotkey failed"), I18N.GetString("Shadowsocks"));
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }
        }
    }
}
