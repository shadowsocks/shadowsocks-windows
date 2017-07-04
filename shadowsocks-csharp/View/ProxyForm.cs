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
        private ProxyConfig _modifiedConfiguration;

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
            UseProxyCheckBox.Text = I18N.GetString("Use Proxy");
            ProxyTypeLabel.Text = I18N.GetString("Proxy Type");
            ProxyAddrLabel.Text = I18N.GetString("Proxy Addr");
            ProxyPortLabel.Text = I18N.GetString("Proxy Port");
            ProxyTimeoutLabel.Text = I18N.GetString("Timeout(Sec)");
            UseAuthCheckBox.Text = I18N.GetString("Use Auth");
            AuthUserLabel.Text = I18N.GetString("Auth User");
            AuthPwdLabel.Text = I18N.GetString("Auth Pwd");
            OKButton.Text = I18N.GetString("OK");
            MyCancelButton.Text = I18N.GetString("Cancel");
            this.Text = I18N.GetString("Edit Proxy");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfigurationCopy().proxy;
            UseProxyCheckBox.Checked = _modifiedConfiguration.useProxy;
            ProxyServerTextBox.Text = _modifiedConfiguration.proxyServer;
            ProxyPortTextBox.Text = _modifiedConfiguration.proxyPort.ToString();
            ProxyTimeoutTextBox.Text = _modifiedConfiguration.proxyTimeout.ToString();
            ProxyTypeComboBox.SelectedIndex = _modifiedConfiguration.proxyType;
            UseAuthCheckBox.Checked = _modifiedConfiguration.useAuth;
            AuthUserTextBox.Text = _modifiedConfiguration.authUser;
            AuthPwdTextBox.Text = _modifiedConfiguration.authPwd;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {

            if (UseProxyCheckBox.Checked)
            {
                int port;
                int timeout;
                if (!int.TryParse(ProxyPortTextBox.Text, out port))
                {
                    MessageBox.Show(I18N.GetString("Illegal port number format"));
                    return;
                }

                if (!int.TryParse(ProxyTimeoutTextBox.Text, out timeout))
                {
                    MessageBox.Show(I18N.GetString("Illegal timeout format"));
                    return;
                }

                var type = ProxyTypeComboBox.SelectedIndex;
                var proxy = ProxyServerTextBox.Text;
                try
                {
                    Configuration.CheckServer(proxy);
                    Configuration.CheckPort(port);
                    Configuration.CheckTimeout(timeout, ProxyConfig.MaxProxyTimeoutSec);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                if (UseAuthCheckBox.Checked)
                {
                    var user = AuthUserTextBox.Text;
                    var pwd = AuthPwdTextBox.Text;
                    try
                    {
                        Configuration.CheckProxyAuthUser(user);
                        Configuration.CheckProxyAuthPwd(pwd);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }

                    controller.EnableProxyWithAuth(type, proxy, port, timeout, user, pwd);
                }
                else
                {
                    controller.EnableProxy(type, proxy, port, timeout);
                }
            }
            else
            {
                controller.DisableProxy();
            }

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

        private void UpdateEnabled()
        {
            if (UseProxyCheckBox.Checked)
            {
                ProxyServerTextBox.Enabled = true;
                ProxyPortTextBox.Enabled = true;
                ProxyTimeoutTextBox.Enabled = true;
                ProxyTypeComboBox.Enabled = true;

                if (ProxyTypeComboBox.SelectedIndex == ProxyConfig.PROXY_HTTP)
                {
                    UseAuthCheckBox.Enabled = true;

                    if (UseAuthCheckBox.Checked)
                    { 
                        AuthUserTextBox.Enabled = true;
                        AuthPwdTextBox.Enabled = true;
                    }
                    else
                    {
                        AuthUserTextBox.Enabled = false;
                        AuthPwdTextBox.Enabled = false;
                    }
                }
                else
                {
                    // TODO support for SOCK5 auth
                    UseAuthCheckBox.Enabled = false;
                    AuthUserTextBox.Enabled = false;
                    AuthPwdTextBox.Enabled = false;
                }
            }
            else
            {
                ProxyServerTextBox.Enabled = false;
                ProxyPortTextBox.Enabled = false;
                ProxyTimeoutTextBox.Enabled = false;
                ProxyTypeComboBox.Enabled = false;
                UseAuthCheckBox.Enabled = false;
                AuthUserTextBox.Enabled = false;
                AuthPwdTextBox.Enabled = false;
            }
        }
    }
}
