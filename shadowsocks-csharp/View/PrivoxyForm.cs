using System;
using System.Drawing;
using System.Windows.Forms;

using Shadowsocks.Model;
using Shadowsocks.Controller;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class PrivoxyForm : Form
    {
        private ShadowsocksController controller;

        public PrivoxyForm(ShadowsocksController controller)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            this.controller = controller;
            controller.ConfigChanged += controller_ConfigChanged;

            UpdateWidgetState();
            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            CustomPrivoxyPort.Text = I18N.GetString("Custom listen port");
            OKButton.Text = I18N.GetString("OK");
            MyCancelButton.Text = I18N.GetString("Cancel");
            this.Text = I18N.GetString("Privoxy Setting");
        }

        private void LoadCurrentConfiguration()
        {
            var _configForMod = controller.GetConfigurationCopy().privoxyConfig;
            CustomPrivoxyPort.Checked = _configForMod.enableCustomPort;
            ListenPort.Text = _configForMod.listenPort.ToString();
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void UpdateWidgetState()
        {
            if (CustomPrivoxyPort.Checked)
            {
                ListenPort.Enabled = true;
            }
            else
            {
                ListenPort.Enabled = false;
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (CustomPrivoxyPort.Checked)
            {
                int port;
                if (!int.TryParse(ListenPort.Text, out port))
                {
                    MessageBox.Show(I18N.GetString("Illegal port number format"));
                    return;
                }

                try
                {
                    Configuration.CheckPort(port);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                controller.SavePrivoxyConfig(true, port);
            }
            else
            {
                controller.SavePrivoxyConfig(false, 0);
            }
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CustomPrivoxyPort_CheckedChanged(object sender, EventArgs e)
        {
            UpdateWidgetState();
        }
    }
}
