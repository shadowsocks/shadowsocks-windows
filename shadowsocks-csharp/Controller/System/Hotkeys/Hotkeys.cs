using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using GlobalHotKey;

namespace Shadowsocks.Controller.Hotkeys
{
    public static class HotKeys
    {
        private static HotKeyManager _hotKeyManager;

        public delegate void HotKeyCallBackHandler();
        // map key and corresponding handler function
        private static Dictionary<HotKey, HotKeyCallBackHandler> _keymap = new Dictionary<HotKey, HotKeyCallBackHandler>();

        private static void LoadConfiguration(Model.HotkeyConfig config)
        {
            if (config == null) return;
            foreach (var fieldInfo in config.GetType().GetFields())
            {
                var fValue = fieldInfo.GetValue(config);
                if (!(fValue is string)) continue;
                var str = fValue as string;
                var hotkey = Str2HotKey(str);
                if (hotkey == null) continue;
                //SwitchSystemProxyMode=>SwitchProxyModeCallback()
                var callbackName = fieldInfo.Name == "SwitchSystemProxyMode" ? "SwitchProxyModeCallback" : fieldInfo.Name + "Callback";
                if (!(HotkeyCallbacks.GetCallback(callbackName) is HotKeyCallBackHandler)) continue;
                var callback = HotkeyCallbacks.GetCallback(callbackName) as HotKeyCallBackHandler;
                HotKey prevHotKey;
                if (IsCallbackExists(callback, out prevHotKey))
                    UnRegist(prevHotKey);
                var regResult = Regist(hotkey, callback);
                Logging.Info(string.Format("HotKey : {0} -> {1} - {2}", str, fieldInfo.Name, regResult ? "Success" : "Failed"));
            }
        }

        public static void Init(ShadowsocksController controller)
        {
            _hotKeyManager = new HotKeyManager();
            _hotKeyManager.KeyPressed += HotKeyManagerPressed;

            HotkeyCallbacks.InitInstance(controller);

            LoadConfiguration(controller.GetConfigurationCopy().hotkey);
        }

        public static void Destroy()
        {
            _hotKeyManager.KeyPressed -= HotKeyManagerPressed;
            _hotKeyManager.Dispose();
        }

        private static void HotKeyManagerPressed(object sender, KeyPressedEventArgs e)
        {
            var hotkey = e.HotKey;
            HotKeyCallBackHandler callback;
            if (_keymap.TryGetValue(hotkey, out callback))
                callback();
        }
        
        public static bool IsHotkeyExists( HotKey hotKey )
        {
            if (hotKey == null) throw new ArgumentNullException(nameof(hotKey));
            return _keymap.Any( v => v.Key.Equals( hotKey ) );
        }

        public static bool IsCallbackExists( HotKeyCallBackHandler cb, out HotKey hotkey)
        {
            if (cb == null) throw new ArgumentNullException(nameof(cb));
            try
            {
                var key = _keymap.First(x => x.Value == cb).Key;
                hotkey = key;
                return true;
            }
            catch (InvalidOperationException)
            {
                // not found
                hotkey = null;
                return false;
            }
        }
        public static string HotKey2Str( HotKey key )
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return HotKey2Str( key.Key, key.Modifiers );
        }

        public static string HotKey2Str( Key key, ModifierKeys modifier )
        {
            if (!Enum.IsDefined(typeof(Key), key))
                throw new InvalidEnumArgumentException(nameof(key), (int) key, typeof(Key));
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

        public static HotKey Str2HotKey(string s)
        {
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
                _keymap[key] = callBack;
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
            if (!Enum.IsDefined(typeof(Key), key))
                throw new InvalidEnumArgumentException(nameof(key), (int) key, typeof(Key));
            try
            {
                var hotkey = _hotKeyManager.Register(key, modifiers);
                _keymap[hotkey] = callBack;
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
            if(_keymap.ContainsKey(key))
                _keymap.Remove(key);
        }
    }
}