using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class SettingsForm : Form
    {
        private ShadowsocksController controller;
        // this is a copy of configuration that we are working on
        private Configuration _modifiedConfiguration;

        public SettingsForm(ShadowsocksController controller)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            this.controller = controller;

            UpdateTexts();
            controller.ConfigChanged += controller_ConfigChanged;

            int dpi_mul = Util.Utils.GetDpiMul();
            int font_height = 9;
            //Font new_font = new Font("Arial", (float)(9.0 * dpi_mul / 4));
            //this.Font = new_font;
            comboProxyType.Height = comboProxyType.Height - font_height + font_height * dpi_mul / 4;
            comboProxyType.Width = comboProxyType.Width * dpi_mul / 4;
            RandomComboBox.Height = RandomComboBox.Height - font_height + font_height * dpi_mul / 4;
            RandomComboBox.Width = RandomComboBox.Width * dpi_mul / 4;

            TextS5Server.Width = TextS5Server.Width * dpi_mul / 4;
            TextS5Port.Width = TextS5Port.Width * dpi_mul / 4;
            TextS5User.Width = TextS5User.Width * dpi_mul / 4;
            TextS5Pass.Width = TextS5Pass.Width * dpi_mul / 4;
            TextUserAgent.Width = TextUserAgent.Width * dpi_mul / 4;

            ProxyPortTextBox.Width = ProxyPortTextBox.Width * dpi_mul / 4;
            TextAuthUser.Width = TextAuthUser.Width * dpi_mul / 4;
            TextAuthPass.Width = TextAuthPass.Width * dpi_mul / 4;

            DNSText.Width = DNSText.Width * dpi_mul / 4;
            ReconnectText.Width = ReconnectText.Width * dpi_mul / 4;
            TTLText.Width = TTLText.Width * dpi_mul / 4;

            LoadCurrentConfiguration();
        }
        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
        }

        private void UpdateTexts()
        {
            this.Text = I18N.GetString("Global Settings") + "("
                + (controller.GetCurrentConfiguration().shareOverLan ? "any" : "local") + ":" + controller.GetCurrentConfiguration().localPort.ToString()
                + I18N.GetString(" Version") + UpdateChecker.FullVersion
                + ")";

            ListenGroup.Text = I18N.GetString(ListenGroup.Text);
            checkShareOverLan.Text = I18N.GetString(checkShareOverLan.Text);
            ProxyPortLabel.Text = I18N.GetString("Proxy Port");
            ReconnectLabel.Text = I18N.GetString("Reconnect Times");
            TTLLabel.Text = I18N.GetString("TTL");
            labelTimeout.Text = I18N.GetString(labelTimeout.Text);

            checkAutoStartup.Text = I18N.GetString(checkAutoStartup.Text);
            checkRandom.Text = I18N.GetString(checkRandom.Text);
            CheckAutoBan.Text = I18N.GetString("AutoBan");

            Socks5ProxyGroup.Text = I18N.GetString(Socks5ProxyGroup.Text);
            checkBoxPacProxy.Text = I18N.GetString(checkBoxPacProxy.Text);
            CheckSockProxy.Text = I18N.GetString("Proxy On");
            LabelS5Server.Text = I18N.GetString("Server IP");
            LabelS5Port.Text = I18N.GetString("Server Port");
            LabelS5Server.Text = I18N.GetString("Server IP");
            LabelS5Port.Text = I18N.GetString("Server Port");
            LabelS5Username.Text = I18N.GetString("Username");
            LabelS5Password.Text = I18N.GetString("Password");
            LabelAuthUser.Text = I18N.GetString("Username");
            LabelAuthPass.Text = I18N.GetString("Password");

            LabelRandom.Text = I18N.GetString("Balance");
            for (int i = 0; i < comboProxyType.Items.Count; ++i)
            {
                comboProxyType.Items[i] = I18N.GetString(comboProxyType.Items[i].ToString());
            }
            for (int i = 0; i < RandomComboBox.Items.Count; ++i)
            {
                RandomComboBox.Items[i] = I18N.GetString(RandomComboBox.Items[i].ToString());
            }

            OKButton.Text = I18N.GetString("OK");
            MyCancelButton.Text = I18N.GetString("Cancel");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void ShowWindow()
        {
            this.Opacity = 1;
            this.Show();
        }

        private int SaveOldSelectedServer()
        {
            try
            {
                int localPort = int.Parse(ProxyPortTextBox.Text);
                Configuration.CheckPort(localPort);
                int ret = 0;
                _modifiedConfiguration.shareOverLan = checkShareOverLan.Checked;
                _modifiedConfiguration.localPort = localPort;
                _modifiedConfiguration.reconnectTimes = ReconnectText.Text.Length == 0 ? 0 : int.Parse(ReconnectText.Text);

                if (checkAutoStartup.Checked != AutoStartup.Check() && !AutoStartup.Set(checkAutoStartup.Checked))
                {
                    MessageBox.Show(I18N.GetString("Failed to update registry"));
                }
                _modifiedConfiguration.random = checkRandom.Checked;
                _modifiedConfiguration.randomAlgorithm = RandomComboBox.SelectedIndex;
                _modifiedConfiguration.TTL = TTLText.Text.Length == 0 ? 0 : int.Parse(TTLText.Text);
                _modifiedConfiguration.connect_timeout = textTimeout.Text.Length == 0 ? 0 : int.Parse(textTimeout.Text);
                _modifiedConfiguration.dns_server = DNSText.Text;
                _modifiedConfiguration.proxyEnable = CheckSockProxy.Checked;
                _modifiedConfiguration.pacDirectGoProxy = checkBoxPacProxy.Checked;
                _modifiedConfiguration.proxyType = comboProxyType.SelectedIndex;
                _modifiedConfiguration.proxyHost = TextS5Server.Text;
                _modifiedConfiguration.proxyPort = TextS5Port.Text.Length == 0 ? 0 : int.Parse(TextS5Port.Text);
                _modifiedConfiguration.proxyAuthUser = TextS5User.Text;
                _modifiedConfiguration.proxyAuthPass = TextS5Pass.Text;
                _modifiedConfiguration.proxyUserAgent = TextUserAgent.Text;
                _modifiedConfiguration.authUser = TextAuthUser.Text;
                _modifiedConfiguration.authPass = TextAuthPass.Text;

                _modifiedConfiguration.autoBan = CheckAutoBan.Checked;

                return ret;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return -1; // ERROR
        }

        private void LoadSelectedServer()
        {
            checkShareOverLan.Checked = _modifiedConfiguration.shareOverLan;
            ProxyPortTextBox.Text = _modifiedConfiguration.localPort.ToString();
            ReconnectText.Text = _modifiedConfiguration.reconnectTimes.ToString();

            checkAutoStartup.Checked = AutoStartup.Check();
            checkRandom.Checked = _modifiedConfiguration.random;
            RandomComboBox.SelectedIndex = _modifiedConfiguration.randomAlgorithm;
            TTLText.Text = _modifiedConfiguration.TTL.ToString();
            textTimeout.Text = _modifiedConfiguration.connect_timeout.ToString();
            DNSText.Text = _modifiedConfiguration.dns_server;

            CheckSockProxy.Checked = _modifiedConfiguration.proxyEnable;
            checkBoxPacProxy.Checked = _modifiedConfiguration.pacDirectGoProxy;
            comboProxyType.SelectedIndex = _modifiedConfiguration.proxyType;
            TextS5Server.Text = _modifiedConfiguration.proxyHost;
            TextS5Port.Text = _modifiedConfiguration.proxyPort.ToString();
            TextS5User.Text = _modifiedConfiguration.proxyAuthUser;
            TextS5Pass.Text = _modifiedConfiguration.proxyAuthPass;
            TextUserAgent.Text = _modifiedConfiguration.proxyUserAgent;
            TextAuthUser.Text = _modifiedConfiguration.authUser;
            TextAuthPass.Text = _modifiedConfiguration.authPass;

            CheckAutoBan.Checked = _modifiedConfiguration.autoBan;
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadSelectedServer();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (SaveOldSelectedServer() == -1)
            {
                return;
            }
            if (_modifiedConfiguration.configs.Count == 0)
            {
                MessageBox.Show(I18N.GetString("Please add at least one server"));
                return;
            }
            controller.SaveServersConfig(_modifiedConfiguration);
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
