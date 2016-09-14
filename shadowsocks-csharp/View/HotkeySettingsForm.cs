using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class HotkeySettingsForm : Form
    {
        private ShadowsocksController _controller;

        // this is a copy of configuration that we are working on
        private HotkeyConfig _modifiedConfig;

        private StringBuilder _sb = new StringBuilder();

        // TODO: not finished
        public HotkeySettingsForm(ShadowsocksController controller)
        {
            InitializeComponent();

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            _controller = controller;
            _controller.ConfigChanged += controller_ConfigChanged;

            LoadCurrentConfiguration();
        }

        private void LoadConfiguration(HotkeyConfig config)
        {
            txtSwitchSystemProxy.Text = config.SwitchSystemProxy;
            txtChangeToPac.Text = config.ChangeToPac;
            txtChangeToGlobal.Text = config.ChangeToGlobal;
            txtSwitchAllowLan.Text = config.SwitchAllowLan;
            txtShowLogs.Text = config.ShowLogs;
            ckbAllowSwitchServer.Checked = config.AllowSwitchServer;
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfig = _controller.GetConfigurationCopy().hotkey;
            if (_modifiedConfig == null)
                _modifiedConfig = new HotkeyConfig();
            LoadConfiguration(_modifiedConfig);
        }

        private void UpdateTexts()
        {
            lblSwitchSystemProxy.Text = I18N.GetString(lblSwitchSystemProxy.Text);
            lblChangeToPac.Text = I18N.GetString(lblChangeToPac.Text);
            lblChangeToGlobal.Text = I18N.GetString(lblChangeToGlobal.Text);
            lblSwitchAllowLan.Text = I18N.GetString(lblSwitchAllowLan.Text);
            lblShowLogs.Text = I18N.GetString(lblShowLogs.Text);
            ckbAllowSwitchServer.Text = I18N.GetString(ckbAllowSwitchServer.Text);
            ok.Text = I18N.GetString(ok.Text);
            cancel.Text = I18N.GetString(cancel.Text);
            this.Text = I18N.GetString(Text);
        }


        /// <summary>
        /// Capture hotkey - Press key
        /// </summary>
        private void HotkeyDown(object sender, KeyEventArgs e)
        {
            _sb.Length = 0;
            //Combination key only
            if (e.Modifiers != 0)
            {
                // XXX: don't change this order
                if (e.Control)
                {
                    _sb.Append("Ctrl + ");
                }
                if (e.Alt)
                {
                    _sb.Append("Alt + ");
                }
                if (e.Shift)
                {
                    _sb.Append("Shift + ");
                }

                Keys keyvalue = (Keys) e.KeyValue;
                if ((keyvalue >= Keys.PageUp && keyvalue <= Keys.Down) ||
                    (keyvalue >= Keys.A && keyvalue <= Keys.Z) ||
                    (keyvalue >= Keys.F1 && keyvalue <= Keys.F12))
                {
                    _sb.Append(e.KeyCode);
                }
                else if (keyvalue >= Keys.D0 && keyvalue <= Keys.D9)
                {
                    _sb.Append('D').Append((char) e.KeyValue);
                }
                else if (keyvalue >= Keys.NumPad0 && keyvalue <= Keys.NumPad9)
                {
                    _sb.Append("NumPad").Append((char) (e.KeyValue - 48));
                }
            }
            ((TextBox) sender).Text = _sb.ToString();
        }

        /// <summary>
        /// Capture hotkey - Release key
        /// </summary>
        private void HotkeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string content = tb.Text.TrimEnd();
            if (content.Length >= 1 && content[content.Length - 1] == '+')
            {
                tb.Text = "";
            }
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void ok_Click(object sender, EventArgs e)
        {
            //Save Config


            var allTextboxes = HotKeys.GetChildControls<TextBox>(this);
            foreach (TextBox tb in allTextboxes)
            {
                if (HotKeys.Str2HotKey(tb.Text.ToString()) == null)
                {
                    // parse err
                    MessageBox.Show("Can not parse");
                    return;
                }
            }
            // try to register keys

            // write into config
            //WriteHotKeyConfig( conf );

            this.Close();
        }

        private void WriteHotKeyConfig(HotkeyConfig conf)
        {
            conf.SwitchSystemProxy = txtSwitchSystemProxy.Text;
            conf.ChangeToPac = txtChangeToPac.Text;
            conf.ChangeToGlobal = txtChangeToGlobal.Text;
            conf.SwitchAllowLan = txtSwitchAllowLan.Text;
            conf.ShowLogs = txtShowLogs.Text;
            conf.AllowSwitchServer = ckbAllowSwitchServer.Checked;
            //controller.SaveHotkeyConfig( conf );
        }

        private void HotkeySettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            HotkeyConfig config = _controller.GetConfigurationCopy().hotkey;
            if (config == null)
                config = new HotkeyConfig();
            config.SwitchSystemProxy = txtSwitchSystemProxy.Text;
            config.ChangeToPac = txtChangeToPac.Text;
            config.ChangeToGlobal = txtChangeToGlobal.Text;
            config.SwitchAllowLan = txtSwitchAllowLan.Text;
            config.ShowLogs = txtShowLogs.Text;
            config.AllowSwitchServer = ckbAllowSwitchServer.Checked;
            _controller.SaveHotKeyConfig(config);
        }
    }
}