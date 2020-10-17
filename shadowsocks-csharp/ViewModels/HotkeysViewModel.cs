using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.View;
using System.Reactive;
using System.Text;
using System.Windows.Input;

namespace Shadowsocks.ViewModels
{
    public class HotkeysViewModel : ReactiveObject
    {
        public HotkeysViewModel()
        {
            _config = Program.MainController.GetCurrentConfiguration();
            _controller = Program.MainController;
            _menuViewController = Program.MenuController;
            
            HotkeySystemProxy = _config.hotkey.SwitchSystemProxy;
            HotkeyProxyMode = _config.hotkey.SwitchSystemProxyMode;
            HotkeyAllowLan = _config.hotkey.SwitchAllowLan;
            HotkeyOpenLogs = _config.hotkey.ShowLogs;
            HotkeySwitchPrev = _config.hotkey.ServerMoveUp;
            HotkeySwitchNext = _config.hotkey.ServerMoveDown;
            RegisterAtStartup = _config.hotkey.RegHotkeysAtStartup;

            HotkeySystemProxyStatus = "✔";
            HotkeyProxyModeStatus = "✔";
            HotkeyAllowLanStatus = "✔";
            HotkeyOpenLogsStatus = "✔";
            HotkeySwitchPrevStatus = "✔";
            HotkeySwitchNextStatus = "✔";

            RegisterAll = ReactiveCommand.Create(() => RegisterAllAndUpdateStatus());
            Save = ReactiveCommand.Create(() => RegisterAllAndUpdateStatus(true));
            Cancel = ReactiveCommand.Create(_menuViewController.CloseHotkeysWindow);
        }

        private readonly Configuration _config;
        private readonly ShadowsocksController _controller;
        private readonly MenuViewController _menuViewController;

        public ReactiveCommand<Unit, Unit> RegisterAll { get; }
        public ReactiveCommand<Unit, Unit> Save { get; }
        public ReactiveCommand<Unit, Unit> Cancel { get; }

        [Reactive]
        public string HotkeySystemProxy { get; set; }

        [Reactive]
        public string HotkeyProxyMode { get; set; }

        [Reactive]
        public string HotkeyAllowLan { get; set; }

        [Reactive]
        public string HotkeyOpenLogs { get; set; }

        [Reactive]
        public string HotkeySwitchPrev { get; set; }

        [Reactive]
        public string HotkeySwitchNext { get; set; }

        [Reactive]
        public bool RegisterAtStartup { get; set; }

        [Reactive]
        public string HotkeySystemProxyStatus { get; set; }

        [Reactive]
        public string HotkeyProxyModeStatus { get; set; }

        [Reactive]
        public string HotkeyAllowLanStatus { get; set; }

        [Reactive]
        public string HotkeyOpenLogsStatus { get; set; }

        [Reactive]
        public string HotkeySwitchPrevStatus { get; set; }

        [Reactive]
        public string HotkeySwitchNextStatus { get; set; }

        public void RecordKeyDown(int hotkeyIndex, KeyEventArgs keyEventArgs)
        {
            var recordedKeyStringBuilder = new StringBuilder();

            // record modifiers
            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                recordedKeyStringBuilder.Append("Ctrl+");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
                recordedKeyStringBuilder.Append("Alt+");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
                recordedKeyStringBuilder.Append("Shift+");

            // record other keys when at least one modifier is pressed
            if (recordedKeyStringBuilder.Length > 0 && (keyEventArgs.Key < Key.LeftShift || keyEventArgs.Key > Key.RightAlt))
                recordedKeyStringBuilder.Append(keyEventArgs.Key);

            switch (hotkeyIndex)
            {
                case 0:
                    HotkeySystemProxy = recordedKeyStringBuilder.ToString();
                    break;
                case 1:
                    HotkeyProxyMode = recordedKeyStringBuilder.ToString();
                    break;
                case 2:
                    HotkeyAllowLan = recordedKeyStringBuilder.ToString();
                    break;
                case 3:
                    HotkeyOpenLogs = recordedKeyStringBuilder.ToString();
                    break;
                case 4:
                    HotkeySwitchPrev = recordedKeyStringBuilder.ToString();
                    break;
                case 5:
                    HotkeySwitchNext = recordedKeyStringBuilder.ToString();
                    break;
            }
        }

        public void FinishOnKeyUp(int hotkeyIndex, KeyEventArgs keyEventArgs)
        {
            switch (hotkeyIndex)
            {
                case 0:
                    if (HotkeySystemProxy.EndsWith("+"))
                        HotkeySystemProxy = "";
                    break;
                case 1:
                    if (HotkeyProxyMode.EndsWith("+"))
                        HotkeyProxyMode = "";
                    break;
                case 2:
                    if (HotkeyAllowLan.EndsWith("+"))
                        HotkeyAllowLan = "";
                    break;
                case 3:
                    if (HotkeyOpenLogs.EndsWith("+"))
                        HotkeyOpenLogs = "";
                    break;
                case 4:
                    if (HotkeySwitchPrev.EndsWith("+"))
                        HotkeySwitchPrev = "";
                    break;
                case 5:
                    if (HotkeySwitchNext.EndsWith("+"))
                        HotkeySwitchNext = "";
                    break;
            }
        }

        private void RegisterAllAndUpdateStatus(bool save = false)
        {
            HotkeySystemProxyStatus = HotkeyReg.RegHotkeyFromString(HotkeySystemProxy, "SwitchSystemProxyCallback") ? "✔" : "❌";
            HotkeyProxyModeStatus = HotkeyReg.RegHotkeyFromString(HotkeyProxyMode, "SwitchSystemProxyModeCallback") ? "✔" : "❌";
            HotkeyAllowLanStatus = HotkeyReg.RegHotkeyFromString(HotkeyAllowLan, "SwitchAllowLanCallback") ? "✔" : "❌";
            HotkeyOpenLogsStatus = HotkeyReg.RegHotkeyFromString(HotkeyOpenLogs, "ShowLogsCallback") ? "✔" : "❌";
            HotkeySwitchPrevStatus = HotkeyReg.RegHotkeyFromString(HotkeySwitchPrev, "ServerMoveUpCallback") ? "✔" : "❌";
            HotkeySwitchNextStatus = HotkeyReg.RegHotkeyFromString(HotkeySwitchNext, "ServerMoveDownCallback") ? "✔" : "❌";

            if (HotkeySystemProxyStatus == "✔" &&
                HotkeyProxyModeStatus == "✔" &&
                HotkeyAllowLanStatus == "✔" &&
                HotkeyOpenLogsStatus == "✔" &&
                HotkeySwitchPrevStatus == "✔" &&
                HotkeySwitchNextStatus == "✔" && save)
            {
                _controller.SaveHotkeyConfig(GetHotkeyConfig);
                _menuViewController.CloseHotkeysWindow();
            }
        }

        private HotkeyConfig GetHotkeyConfig => new HotkeyConfig()
        {
            SwitchSystemProxy = HotkeySystemProxy,
            SwitchSystemProxyMode = HotkeyProxyMode,
            SwitchAllowLan = HotkeyAllowLan,
            ShowLogs = HotkeyOpenLogs,
            ServerMoveUp = HotkeySwitchPrev,
            ServerMoveDown = HotkeySwitchNext,
            RegHotkeysAtStartup = RegisterAtStartup
        };
    }
}
