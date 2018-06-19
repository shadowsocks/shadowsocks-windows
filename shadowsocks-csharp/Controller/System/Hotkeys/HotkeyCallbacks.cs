using System;
using System.Reflection;

namespace Shadowsocks.Controller.Hotkeys
{
    public class HotkeyCallbacks
    {

        public static void InitInstance(ShadowsocksController controller)
        {
            if (Instance != null)
            {
                return;
            }

            Instance = new HotkeyCallbacks(controller);
        }

        /// <summary>
        /// Create hotkey callback handler delegate based on callback name
        /// </summary>
        /// <param name="methodname"></param>
        /// <returns></returns>
        public static Delegate GetCallback(string methodname)
        {
            if (methodname.IsNullOrEmpty()) throw new ArgumentException(nameof(methodname));
            MethodInfo dynMethod = typeof(HotkeyCallbacks).GetMethod(methodname,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return dynMethod == null ? null : Delegate.CreateDelegate(typeof(HotKeys.HotKeyCallBackHandler), Instance, dynMethod);
        }

        #region Singleton 
        
        private static HotkeyCallbacks Instance { get; set; }

        private readonly ShadowsocksController _controller;

        private HotkeyCallbacks(ShadowsocksController controller)
        {
            _controller = controller;
        }

        #endregion

        #region Callbacks

        private void SwitchSystemProxyCallback()
        {
            bool enabled = _controller.GetConfigurationCopy().enabled;
            _controller.ToggleEnable(!enabled);
        }

        private void SwitchProxyModeCallback()
        {
            var config = _controller.GetConfigurationCopy();
            if (config.enabled == false) return;
            var currStatus = config.global;
            _controller.ToggleGlobal(!currStatus);
        }

        private void SwitchAllowLanCallback()
        {
            var status = _controller.GetConfigurationCopy().shareOverLan;
            _controller.ToggleShareOverLAN(!status);
        }

        private void ShowLogsCallback()
        {
            Program.MenuController.ShowLogForm_HotKey();
        }

        private void ServerMoveUpCallback()
        {
            int currIndex;
            int serverCount;
            GetCurrServerInfo(out currIndex, out serverCount);
            if (currIndex - 1 < 0)
            {
                // revert to last server
                currIndex = serverCount - 1;
            }
            else
            {
                currIndex -= 1;
            }
            _controller.SelectServerIndex(currIndex);
        }

        private void ServerMoveDownCallback()
        {
            int currIndex;
            int serverCount;
            GetCurrServerInfo(out currIndex, out serverCount);
            if (currIndex + 1 == serverCount)
            {
                // revert to first server
                currIndex = 0;
            }
            else
            {
                currIndex += 1;
            }
            _controller.SelectServerIndex(currIndex);
        }

        private void GetCurrServerInfo(out int currIndex, out int serverCount)
        {
            var currConfig = _controller.GetCurrentConfiguration();
            currIndex = currConfig.index;
            serverCount = currConfig.configs.Count;
        }

        #endregion
    }
}
