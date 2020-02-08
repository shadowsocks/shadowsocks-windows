using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class QRCodeForm : Form
    {
        private string code;

        public QRCodeForm(string code)
        {
            this.code = code;
            InitializeComponent();
            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            Text = I18N.GetString("QRCode and URL");
        }

        private void GenQR(string ssconfig)
        {
            (Bitmap img, int blockSize) = QRCodeUtil.GenerateQRCode(ssconfig, new Size
            {
                Height = qrcodeBox.Height,
                Width = qrcodeBox.Width
            });
            int dWidth = qrcodeBox.Width - img.Width;
            int dHeight = qrcodeBox.Height - img.Height;
            int maxD = Math.Max(dWidth, dHeight);
            qrcodeBox.SizeMode = maxD >= 7 * blockSize ? PictureBoxSizeMode.Zoom : PictureBoxSizeMode.CenterImage;
            qrcodeBox.Image = img;
        }

        private void QRCodeForm_Load(object sender, EventArgs e)
        {
            Configuration servers = Configuration.Load();
            List<KeyValuePair<string, string>> serverDatas = servers.configs.Select(
                server =>
                    new KeyValuePair<string, string>(ShadowsocksController.GetServerURL(server), server.ToString())
                ).ToList();
            serverListBox.DataSource = serverDatas;

            int selectIndex = serverDatas.FindIndex(serverData => serverData.Key.StartsWith(code));
            if (selectIndex >= 0)
            {
                serverListBox.SetSelected(selectIndex, true);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string url = (sender as ListBox)?.SelectedValue.ToString();
            GenQR(url);
            urlTextBox.Text = url;
        }

        private void textBoxURL_Click(object sender, EventArgs e)
        {
            urlTextBox.SelectAll();
        }
    }
}
