using System;
using System.Drawing;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class ProxyForm : Form
    {
        private ShadowsocksController controller;

        // this is a copy of configuration that we are working on
        private ProxyConfig _modifiedProxyConfig;

        public ProxyForm(ShadowsocksController controller)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            this.controller = controller;
            controller.ConfigChanged += controller_ConfigChanged;

            UpdateEnabled();
            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            I18N.TranslateForm(this);
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedProxyConfig = controller.GetConfigurationCopy().proxy;
            UseProxyCheckBox.Checked = _modifiedProxyConfig.useProxy;
            ProxyServerTextBox.Text = _modifiedProxyConfig.proxyServer;
            ProxyPortTextBox.Text = _modifiedProxyConfig.proxyPort.ToString();
            ProxyTimeoutTextBox.Text = _modifiedProxyConfig.proxyTimeout.ToString();
            ProxyTypeComboBox.SelectedIndex = _modifiedProxyConfig.proxyType;
            UseAuthCheckBox.Checked = _modifiedProxyConfig.useAuth;
            AuthUserTextBox.Text = _modifiedProxyConfig.authUser;
            AuthPwdTextBox.Text = _modifiedProxyConfig.authPwd;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            _modifiedProxyConfig.useProxy = UseProxyCheckBox.Checked;
            if (_modifiedProxyConfig.useProxy)
            {
                if (!int.TryParse(ProxyPortTextBox.Text, out _modifiedProxyConfig.proxyPort))
                {
                    MessageBox.Show(I18N.GetString("Illegal port number format"));
                    return;
                }

                if (!int.TryParse(ProxyTimeoutTextBox.Text, out _modifiedProxyConfig.proxyTimeout))
                {
                    MessageBox.Show(I18N.GetString("Illegal timeout format"));
                    return;
                }

                _modifiedProxyConfig.proxyType = ProxyTypeComboBox.SelectedIndex;

                try
                {
                    Configuration.CheckServer(_modifiedProxyConfig.proxyServer = ProxyServerTextBox.Text);
                    Configuration.CheckPort(_modifiedProxyConfig.proxyPort);
                    Configuration.CheckTimeout(_modifiedProxyConfig.proxyTimeout, ProxyConfig.MaxProxyTimeoutSec);

                    _modifiedProxyConfig.useAuth = UseAuthCheckBox.Checked;
                    if (_modifiedProxyConfig.useAuth)
                    {
                        Configuration.CheckProxyAuthUser(_modifiedProxyConfig.authUser = AuthUserTextBox.Text);
                        Configuration.CheckProxyAuthPwd(_modifiedProxyConfig.authPwd = AuthPwdTextBox.Text);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }

            controller.SaveProxy(_modifiedProxyConfig);

            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ProxyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
        }

        private void UseProxyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            if (UseProxyCheckBox.Checked)
            {
                ProxyServerTextBox.Enabled =
                ProxyPortTextBox.Enabled =
                ProxyTimeoutTextBox.Enabled =
                ProxyTypeComboBox.Enabled = true;

                if (ProxyTypeComboBox.SelectedIndex == ProxyConfig.PROXY_HTTP)
                {
                    UseAuthCheckBox.Enabled = true;

                    if (UseAuthCheckBox.Checked)
                    {
                        AuthUserTextBox.Enabled =
                        AuthPwdTextBox.Enabled = true;
                    }
                    else
                    {
                        AuthUserTextBox.Enabled =
                        AuthPwdTextBox.Enabled = false;
                    }
                }
                else
                {
                    // TODO support for SOCK5 auth
                    UseAuthCheckBox.Enabled =
                    AuthUserTextBox.Enabled =
                    AuthPwdTextBox.Enabled = false;
                }
            }
            else
            {
                ProxyServerTextBox.Enabled =
                ProxyPortTextBox.Enabled =
                ProxyTimeoutTextBox.Enabled =
                ProxyTypeComboBox.Enabled =
                UseAuthCheckBox.Enabled =
                AuthUserTextBox.Enabled =
                AuthPwdTextBox.Enabled = false;
            }
        }

        private void ProxyTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO support for SOCK5 auth
            if (ProxyTypeComboBox.SelectedIndex != ProxyConfig.PROXY_HTTP)
            {
                UseAuthCheckBox.Checked = false;
                AuthUserTextBox.Clear();
                AuthPwdTextBox.Clear();
            }

            UpdateEnabled();
        }

        private void UseAuthCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnabled();
        }
    }
}
