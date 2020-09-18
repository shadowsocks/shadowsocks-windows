using ZXing.QrCode.Internal;
using Shadowsocks.Controller;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Model;

namespace Shadowsocks.View
{
    public partial class QRCodeForm : Form
    {
        private string code;

        public QRCodeForm(string code)
        {
            this.code = code;
            InitializeComponent();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            this.Text = I18N.GetString("QRCode and URL");
        }

        private void QRCodeForm_Load(object sender, EventArgs e)
        {

        }
    }
}
