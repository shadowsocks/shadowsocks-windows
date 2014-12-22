using QRCode4CS;
using Shadowsocks.Controller;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
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
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            this.Text = I18N.GetString("QRCode");
        }

        private void GenQR(string ssconfig)
        {
            string qrText = ssconfig;
            QRCode4CS.Options options = new QRCode4CS.Options();
            options.Text = qrText;
            QRCode4CS.QRCode qrCoded = null;
            bool success = false;
            foreach (var level in new QRErrorCorrectLevel[]{QRErrorCorrectLevel.H, QRErrorCorrectLevel.Q, QRErrorCorrectLevel.M, QRErrorCorrectLevel.L})
            {
                for (int i = 3; i < 10; i++)
                {
                    try
                    {
                        options.TypeNumber = i;
                        options.CorrectLevel = level;
                        qrCoded = new QRCode4CS.QRCode(options);
                        qrCoded.Make();
                        success = true;
                        break;
                    }
                    catch
                    {
                        qrCoded = null;
                        continue;
                    }
                }
                if (success)
                    break;
            }
            if (qrCoded == null)
            {
                return;
            }
            int blockSize = Math.Max(200 / qrCoded.GetModuleCount(), 1);
            Bitmap drawArea = new Bitmap((qrCoded.GetModuleCount() * blockSize), (qrCoded.GetModuleCount() * blockSize));
            using (Graphics g = Graphics.FromImage(drawArea))
            {
                g.Clear(Color.White);
                using (Brush b = new SolidBrush(Color.Black))
                {
                    for (int row = 0; row < qrCoded.GetModuleCount(); row++)
                    {
                        for (int col = 0; col < qrCoded.GetModuleCount(); col++)
                        {
                            if (qrCoded.IsDark(row, col))
                            {
                                g.FillRectangle(b, blockSize * row, blockSize * col, blockSize, blockSize);
                            }
                        }
                    }
                }
            }
            pictureBox1.Image = drawArea;
        }

        private void QRCodeForm_Load(object sender, EventArgs e)
        {
            GenQR(code);
        }
    }
}
