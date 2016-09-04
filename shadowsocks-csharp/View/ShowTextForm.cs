using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class ShowTextForm : Form
    {
        public ShowTextForm(string title, string text)
        {
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            InitializeComponent();

            this.Text = title;
            textBox.Text = text;
        }
    }
}
