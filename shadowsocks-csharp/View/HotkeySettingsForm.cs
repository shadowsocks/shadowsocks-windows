using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using Shadowsocks.Controller;
using Shadowsocks.Controller.Hotkeys;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.View
{
    public partial class HotkeySettingsForm : Form
    {
        private readonly ShadowsocksController _controller;

        // this is a copy of hotkey configuration that we are working on
        private HotkeyConfig _modifiedHotkeyConfig;

        public HotkeySettingsForm(ShadowsocksController controller)
        {
            InitializeComponent();
            UpdateTexts();
            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            _controller = controller;
            _controller.ConfigChanged += controller_ConfigChanged;

            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            // I18N stuff
            SwitchSystemProxyLabel.Text = I18N.GetString("Switch system proxy");
            SwitchProxyModeLabel.Text = I18N.GetString("Switch system proxy mode");
            SwitchAllowLanLabel.Text = I18N.GetString("Switch share over LAN");
            ShowLogsLabel.Text = I18N.GetString("Show Logs");
            ServerMoveUpLabel.Text = I18N.GetString("Switch to prev server");
            ServerMoveDownLabel.Text = I18N.GetString("Switch to next server");
            btnOK.Text = I18N.GetString("OK");
            btnCancel.Text = I18N.GetString("Cancel");
            btnRegisterAll.Text = I18N.GetString("Reg All");
            this.Text = I18N.GetString("Edit Hotkeys...");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedHotkeyConfig = _controller.GetConfigurationCopy().hotkey;
            SetConfigToUI(_modifiedHotkeyConfig);
        }

        private void SetConfigToUI(HotkeyConfig config)
        {
            SwitchSystemProxyTextBox.Text = config.SwitchSystemProxy;
            SwitchProxyModeTextBox.Text = config.SwitchSystemProxyMode;
            SwitchAllowLanTextBox.Text = config.SwitchAllowLan;
            ShowLogsTextBox.Text = config.ShowLogs;
            ServerMoveUpTextBox.Text = config.ServerMoveUp;
            ServerMoveDownTextBox.Text = config.ServerMoveDown;
        }

        private void SaveConfig()
        {
            _controller.SaveHotkeyConfig(_modifiedHotkeyConfig);
        }

        private HotkeyConfig GetConfigFromUI()
        {
            return new HotkeyConfig
            {
                SwitchSystemProxy = SwitchSystemProxyTextBox.Text,
                SwitchSystemProxyMode = SwitchProxyModeTextBox.Text,
                SwitchAllowLan = SwitchAllowLanTextBox.Text,
                ShowLogs = ShowLogsTextBox.Text,
                ServerMoveUp = ServerMoveUpTextBox.Text,
                ServerMoveDown = ServerMoveDownTextBox.Text
            };
        }

        /// <summary>
        /// Capture hotkey - Press key
        /// </summary>
        private void HotkeyDown(object sender, KeyEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            //Combination key only
            if (e.Modifiers != 0)
            {
                // XXX: Hotkey parsing depends on the sequence, more specifically, ModifierKeysConverter.
                // Windows key is reserved by operating system, we deny this key.
                if (e.Control)
                {
                    sb.Append("Ctrl+");
                }
                if (e.Alt)
                {
                    sb.Append("Alt+");
                }
                if (e.Shift)
                {
                    sb.Append("Shift+");
                }

                Keys keyvalue = (Keys)e.KeyValue;
                if ((keyvalue >= Keys.PageUp && keyvalue <= Keys.Down) ||
                    (keyvalue >= Keys.A && keyvalue <= Keys.Z) ||
                    (keyvalue >= Keys.F1 && keyvalue <= Keys.F12))
                {
                    sb.Append(e.KeyCode);
                }
                else if (keyvalue >= Keys.D0 && keyvalue <= Keys.D9)
                {
                    sb.Append('D').Append((char)e.KeyValue);
                }
                else if (keyvalue >= Keys.NumPad0 && keyvalue <= Keys.NumPad9)
                {
                    sb.Append("NumPad").Append((char)(e.KeyValue - 48));
                }
            }
            ((TextBox)sender).Text = sb.ToString();
        }

        /// <summary>
        /// Capture hotkey - Release key
        /// </summary>
        private void HotkeyUp(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            var content = tb.Text.TrimEnd();
            if (content.Length >= 1 && content[content.Length - 1] == '+')
            {
                tb.Text = "";
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            _modifiedHotkeyConfig = GetConfigFromUI();
            // try to register, notify to change settings if failed
            if (!RegisterAllHotkeys(_modifiedHotkeyConfig))
            {
                MessageBox.Show(I18N.GetString("Register hotkey failed"));
            }

            // All check passed, saving
            SaveConfig();
            this.Close();
        }

        private void RegisterAllButton_Click(object sender, EventArgs e)
        {
            _modifiedHotkeyConfig = GetConfigFromUI();
            RegisterAllHotkeys(_modifiedHotkeyConfig);
        }

        private bool RegisterAllHotkeys(HotkeyConfig hotkeyConfig)
        {
            return
                RegHotkeyFromString(hotkeyConfig.SwitchSystemProxy, "SwitchSystemProxyCallback", SwitchSystemProxyLabel)
                && RegHotkeyFromString(hotkeyConfig.SwitchSystemProxyMode, "SwitchProxyModeCallback", SwitchProxyModeLabel)
                && RegHotkeyFromString(hotkeyConfig.SwitchAllowLan, "SwitchAllowLanCallback", SwitchAllowLanLabel)
                && RegHotkeyFromString(hotkeyConfig.ShowLogs, "ShowLogsCallback", ShowLogsLabel)
                && RegHotkeyFromString(hotkeyConfig.ServerMoveUp, "ServerMoveUpCallback", ServerMoveUpLabel)
                && RegHotkeyFromString(hotkeyConfig.ServerMoveDown, "ServerMoveDownCallback", ServerMoveDownLabel);
        }

        private bool RegHotkeyFromString(string hotkeyStr, string callbackName, Label indicator = null)
        {
            var _callback = HotkeyCallbacks.GetCallback(callbackName);
            if (_callback == null)
            {
                throw new Exception($"{callbackName} not found");
            }

            var callback = _callback as HotKeys.HotKeyCallBackHandler;

            if (hotkeyStr.IsNullOrEmpty())
            {
                HotKeys.UnregExistingHotkey(callback);
                if (indicator != null)
                {
                    indicator.ResetBackColor();
                }
                return true;
            }
            else
            {
                var hotkey = HotKeys.Str2HotKey(hotkeyStr);
                if (hotkey == null)
                {
                    MessageBox.Show(string.Format(I18N.GetString("Cannot parse hotkey: {0}"), hotkeyStr));
                    return false;
                }
                else
                {
                    bool regResult = (HotKeys.RegHotkey(hotkey, callback));
                    if (indicator != null)
                    {
                        indicator.BackColor = regResult ? Color.Green : Color.Yellow;
                    }
                    return regResult;
                }
            }

        }
    }
}