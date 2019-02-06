using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using static Shadowsocks.Controller.HotkeyReg;

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
            RegHotkeysAtStartupLabel.Text = I18N.GetString("Reg Hotkeys At Startup");
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
            RegHotkeysAtStartupCheckBox.Checked = config.RegHotkeysAtStartup;
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
                ServerMoveDown = ServerMoveDownTextBox.Text,
                RegHotkeysAtStartup = RegHotkeysAtStartupCheckBox.Checked
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
                RegHotkeyFromString(hotkeyConfig.SwitchSystemProxy, "SwitchSystemProxyCallback", result => HandleRegResult(hotkeyConfig.SwitchSystemProxy, SwitchSystemProxyLabel, result))
                && RegHotkeyFromString(hotkeyConfig.SwitchSystemProxyMode, "SwitchSystemProxyModeCallback", result => HandleRegResult(hotkeyConfig.SwitchSystemProxyMode, SwitchProxyModeLabel, result))
                && RegHotkeyFromString(hotkeyConfig.SwitchAllowLan, "SwitchAllowLanCallback", result => HandleRegResult(hotkeyConfig.SwitchAllowLan, SwitchAllowLanLabel, result))
                && RegHotkeyFromString(hotkeyConfig.ShowLogs, "ShowLogsCallback", result => HandleRegResult(hotkeyConfig.ShowLogs, ShowLogsLabel, result))
                && RegHotkeyFromString(hotkeyConfig.ServerMoveUp, "ServerMoveUpCallback", result => HandleRegResult(hotkeyConfig.ServerMoveUp, ServerMoveUpLabel, result))
                && RegHotkeyFromString(hotkeyConfig.ServerMoveDown, "ServerMoveDownCallback", result => HandleRegResult(hotkeyConfig.ServerMoveDown, ServerMoveDownLabel, result));
        }

        private void HandleRegResult(string hotkeyStr, Label label, RegResult result)
        {
            switch (result)
            {
                case RegResult.ParseError:
                    MessageBox.Show(I18N.GetString("Cannot parse hotkey: {0}", hotkeyStr));
                    break;
                case RegResult.UnregSuccess:
                    label.ResetBackColor();
                    break;
                case RegResult.RegSuccess:
                    label.BackColor = Color.Green;
                    break;
                case RegResult.RegFailure:
                    label.BackColor = Color.Red;
                    break;
                default:
                    break;
            }
        }
    }
}