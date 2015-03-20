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
    public partial class PACUrlForm : Form
    {
        private ShadowsocksController controller;
        private string orig_pacUrl;

        public PACUrlForm(ShadowsocksController controller)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            this.controller = controller;
            controller.ConfigChanged += controller_ConfigChanged;
        }

        private void UpdateTexts()
        {
            OkButton.Text = I18N.GetString("OK");
            CancelButton.Text = I18N.GetString("Cancel");
            PACUrlLabel.Text = I18N.GetString("PAC Url");
            this.Text = I18N.GetString("Update Online PAC URL");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            orig_pacUrl = PACUrlTextBox.Text = controller.GetConfiguration().pacUrl;
        }

        private void PACUrlForm_Load(object sender, EventArgs e)
        {
            PACUrlTextBox.Text = controller.GetConfiguration().pacUrl;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            string pacUrl = PACUrlTextBox.Text.Trim();
            if (string.IsNullOrEmpty(pacUrl))
            {
                MessageBox.Show(I18N.GetString("PAC Url can not be blank"));
                return;
            }
            if (pacUrl != this.orig_pacUrl)
            {
                controller.SavePACUrl(pacUrl);
            }
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
