using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;


namespace Shadowsocks.View
{
    public partial class URIParseForm : Form
    {
        public bool Parsed { get; set; }
        public Server server;
        public URIParseForm()
        {
            Parsed = false;

            server = Configuration.GetDefaultServer();

            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            // a dirty hack
            this.PerformLayout();

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
        }
        private void UpdateTexts()
        {
            this.URIParseButton.Text = I18N.GetString("&Parse");
            this.URIParseExit.Text = I18N.GetString("Quit");
            this.URILable.Text = I18N.GetString("Shadowsocks URI");
            this.Text = I18N.GetString("Shadowsocks URI Parse");
        }

        private void URIParseButton_Click(object sender, EventArgs e)
        {
            Parsed = Configuration.parse_uri(URITextBox.Text, ref server);

            if (!Parsed)
                server = null;

            this.Close();
        }

        private void URIParseExit_Click(object sender, EventArgs e)
        {
            Parsed = false;
            server = null;
            this.Close();
        }
    }
}
