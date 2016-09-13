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

        public static bool IsExist( HotKey hotKey ) { return keymap.Any( v => v.Key.Equals( hotKey ) ); }

        public static string HotKey2str( HotKey key ) {
            var keyNum = ( int ) key.Key;
            var modifierNum = ( int ) key.Modifiers;
            return $"{keyNum}|{modifierNum}";
        }

        public static string HotKey2str( Key key, ModifierKeys modifier ) {
            var keyNum = ( int ) key;
            var modifierNum = ( int ) modifier;
            return $"{keyNum}|{modifierNum}";
        }

        public static HotKey ParseHotKey( string s ) {
            if (s.IsNullOrEmpty()) return null;
            string[] strings = s.Split( '|' );
            var key = (Key)int.Parse(strings[ 0 ]);
            var modifierCombination = (ModifierKeys)int.Parse(strings[ 1 ]);
            if ( ! ModifierKeysConverter.IsDefinedModifierKeys( modifierCombination ) ) return null;
            return new HotKey(key, modifierCombination);
        }

        public static string DisplayHotKey( HotKey key ) { return DisplayHotKey( key.Key, key.Modifiers ); }

        public static string DisplayHotKey( Key key, ModifierKeys modifier ) {
            string str = "";
            if ( modifier.HasFlag( ModifierKeys.Control ) )
                str += "Ctrl + ";
            else if ( modifier.HasFlag( ModifierKeys.Shift ) )
                str += "Shift + ";
            else if ( modifier.HasFlag( ModifierKeys.Alt ) )
                str += "Alt + ";
            // In general, Win key is reserved by operating system
            // It leaves here just for sanity
            else if ( modifier.HasFlag( ModifierKeys.Windows ) )
                str += "Win + ";
            str += key.ToString();
            return str;
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