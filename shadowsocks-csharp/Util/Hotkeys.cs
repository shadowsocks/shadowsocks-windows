using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using GlobalHotKey;
using Shadowsocks.Controller;

namespace Shadowsocks.Util
{
    public static class HotKeys
    {
        private static HotKeyManager _hotKeyManager;

        public delegate void HotKeyCallBackHandler();
        // map key and corresponding handler function
        private static Dictionary<HotKey, HotKeyCallBackHandler> keymap = new Dictionary<HotKey, HotKeyCallBackHandler>();

        public static void Init()
        {
            _hotKeyManager = new HotKeyManager();
            _hotKeyManager.KeyPressed += HotKeyManagerPressed;
        }

        public static void Destroy()
        {
            // He will unreg all keys and dispose resources
            _hotKeyManager.Dispose();
        }

        static void HotKeyManagerPressed(object sender, KeyPressedEventArgs e)
        {
            var hotkey = e.HotKey;
            HotKeyCallBackHandler callback;
            if (keymap.TryGetValue(hotkey, out callback))
                callback();
        }

        public static bool IsExist(HotKey hotKey)
        {
            return keymap.ContainsKey(hotKey);
        }

        public static bool IsExist(Key key, ModifierKeys modifiers)
        {
            return keymap.ContainsKey(new HotKey(key, modifiers));
        }

        public static int Regist(Key key, ModifierKeys modifiers, HotKeyCallBackHandler callBack)
        {
            try
            {
                _hotKeyManager.Register(key, modifiers);
                var hotkey = new HotKey(key, modifiers);
                if (IsExist(hotkey))
                {
                    // already registered
                    return -3;
                }
                keymap[hotkey] = callBack;
                return 0;
            }
            catch (ArgumentException)
            {
                // already registered
                // notify user to change key
                return -1;
            }
            catch (Win32Exception win32Exception)
            {
                // WinAPI error
                Logging.LogUsefulException(win32Exception);
                return -2;
            }
        }

        public static void UnRegist(HotKey key)
        {
            _hotKeyManager.Unregister(key);
        }

        public static void UnRegist(Key key, ModifierKeys modifiers)
        {
            _hotKeyManager.Unregister(key, modifiers);
        }
    }
}