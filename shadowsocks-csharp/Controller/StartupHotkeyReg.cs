using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GlobalHotKey;
using Shadowsocks.Controller.Hotkeys;
using Shadowsocks.Model;
using System.Reflection;

namespace Shadowsocks.Controller
{
    static class StartupHotkeyReg
    {
        public static void RegHotkey()
        {
            var _hotKeyConf = Configuration.Load().hotkey;

            if (_hotKeyConf == null || !_hotKeyConf.RegHotkeysAtStartup)
                return;

            var _hotKeyDic = new Dictionary<HotKey, HotKeys.HotKeyCallBackHandler>();

            try
            {
                MethodInfo[] fis = typeof(HotkeyCallbacks).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                Type ht = _hotKeyConf.GetType();
                for (int i = 0; i < fis.Length; i++)
                {
                    if (fis[i].Name.EndsWith("Callback"))
                    {
                        var callbackName = fis[i].Name;
                        var fieldName = callbackName.Replace("Callback", "");

                        var hk = HotKeys.Str2HotKey(ht.GetField(fieldName).GetValue(_hotKeyConf) as string);
                        var cb = HotkeyCallbacks.GetCallback(callbackName) as HotKeys.HotKeyCallBackHandler;

                        if (hk != null && cb != null)
                        {
                            _hotKeyDic.Add(hk, cb);
                        }
                    }
                }

                int regCount = 0;
                foreach (var v in _hotKeyDic)
                {
                    if (!HotKeys.RegHotkey(v.Key, v.Value))
                    {
                        foreach (var k in _hotKeyDic)
                        {
                            if (regCount > 0)
                            {
                                HotKeys.UnregExistingHotkey(k.Value);
                                regCount--;
                            }
                        }
                        MessageBox.Show(I18N.GetString("Register hotkey failed"), I18N.GetString("Shadowsocks"));
                        return;
                    }
                    regCount++;
                }
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }
        }
    }
}
