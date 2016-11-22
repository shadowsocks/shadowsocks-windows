using System.Reflection;
using Shadowsocks.View;

namespace Shadowsocks.Controller.Hotkeys
{
    public class HotkeyCallbacks
    {
        public static HotkeyCallbacks Instance { get; private set; }

        public static void InitInstance(ShadowsocksController controller)
        {
            if (Instance != null)
            {
                return;
            }

            Instance = new HotkeyCallbacks(controller);
        }

        private readonly ShadowsocksController _controller;

        private HotkeyCallbacks(ShadowsocksController controller)
        {
            _controller = controller;
        }

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
            // Get the current MenuViewController in this program via reflection
            FieldInfo fi = Assembly.GetExecutingAssembly().GetType("Shadowsocks.Program")
                .GetField("_viewController",
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase);
            // To retrieve the value of a static field, pass null here
            var mvc = fi.GetValue(null) as MenuViewController;
            mvc.ShowLogForm_HotKey();
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
