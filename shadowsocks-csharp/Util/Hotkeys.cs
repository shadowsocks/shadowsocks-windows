using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

        private static void HotKeyManagerPressed(object sender, KeyPressedEventArgs e)
        {
            var hotkey = e.HotKey;
            HotKeyCallBackHandler callback;
            if (keymap.TryGetValue(hotkey, out callback))
                callback();
        }
        
        public static bool IsExist( HotKey hotKey ) { return keymap.Any( v => v.Key.Equals( hotKey ) ); }

        public static string HotKey2Str( HotKey key ) { return HotKey2Str( key.Key, key.Modifiers ); }

        public static string HotKey2Str( Key key, ModifierKeys modifier ) {
            try
            {
                ModifierKeysConverter mkc = new ModifierKeysConverter();
                var keyStr = Enum.GetName(typeof(Key), key);
                var modifierStr = mkc.ConvertToInvariantString(modifier);

                return $"{modifierStr}+{keyStr}";
            }
            catch (NotSupportedException)
            {
                // converter exception
                return null;
            }
        }

        public static HotKey Str2HotKey( string s ) {
            try
            {
                if (s.IsNullOrEmpty()) return null;
                int offset = s.LastIndexOf("+", StringComparison.OrdinalIgnoreCase);
                if (offset <= 0) return null;
                string modifierStr = s.Substring(0, offset).Trim();
                string keyStr = s.Substring(offset + 1).Trim();

                KeyConverter kc = new KeyConverter();
                ModifierKeysConverter mkc = new ModifierKeysConverter();
                Key key = (Key) kc.ConvertFrom(keyStr.ToUpper());
                ModifierKeys modifier = (ModifierKeys) mkc.ConvertFrom(modifierStr.ToUpper());

                return new HotKey(key, modifier);
            }
            catch (NotSupportedException)
            {
                // converter exception
                return null;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public static bool Regist( HotKey key, HotKeyCallBackHandler callBack )
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (callBack == null)
                throw new ArgumentNullException(nameof(callBack));
            try
            {
                _hotKeyManager.Register(key);
                keymap[key] = callBack;
                return true;
            }
            catch (ArgumentException)
            {
                // already called this method with the specific hotkey
                // return success silently
                return true;
            }
            catch (Win32Exception)
            {
                // this hotkey already registered by other programs
                // notify user to change key
                return false;
            }
        }

        public static bool Regist(Key key, ModifierKeys modifiers, HotKeyCallBackHandler callBack)
        {
            try
            {
                var hotkey = _hotKeyManager.Register(key, modifiers);
                keymap[hotkey] = callBack;
                return true;
            }
            catch (ArgumentException)
            {
                // already called this method with the specific hotkey
                // return success silently
                return true;
            }
            catch (Win32Exception)
            {
                // already registered by other programs
                // notify user to change key
                return false;
            }
        }

        public static void UnRegist(HotKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            _hotKeyManager.Unregister(key);
            if(keymap.ContainsKey(key))
                keymap.Remove(key);
        }

        public static IEnumerable<TControl> GetChildControls<TControl>(this Control control) where TControl : Control
        {
            var children = (control.Controls != null) ? control.Controls.OfType<TControl>() : Enumerable.Empty<TControl>();
            return children.SelectMany(c => GetChildControls<TControl>(c)).Concat(children);
        }

        // callbacks
        
    }
}