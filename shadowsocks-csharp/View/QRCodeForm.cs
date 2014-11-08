using QRCode4CS;
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
        }

        private void GenQR(string ssconfig)
        {
            string qrText = ssconfig;
            QRCode4CS.QRCode qrCoded = new QRCode4CS.QRCode(6, QRErrorCorrectLevel.M);
            qrCoded.AddData(qrText);
            qrCoded.Make();
            int blockSize = 5;
            Bitmap drawArea = new Bitmap((qrCoded.GetModuleCount() * blockSize), (qrCoded.GetModuleCount() * blockSize));
            for (int row = 0; row < qrCoded.GetModuleCount(); row++)
            {
                for (int col = 0; col < qrCoded.GetModuleCount(); col++)
                {
                    bool isDark = qrCoded.IsDark(row, col);
                    if (isDark)
                    {
                        for (int y = 0; y < blockSize; y++)
                        {
                            int myCol = (blockSize * (col - 1)) + (y + blockSize);
                            for (int x = 0; x < blockSize; x++)
                            {
                                drawArea.SetPixel((blockSize * (row - 1)) + (x + blockSize), myCol, Color.Black);
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < blockSize; y++)
                        {
                            int myCol = (blockSize * (col - 1)) + (y + blockSize);
                            for (int x = 0; x < blockSize; x++)
                            {
                                drawArea.SetPixel((blockSize * (row - 1)) + (x + blockSize), myCol, Color.White);
                            }
                        }
                    }
                }
            }
            pictureBox1.Image = drawArea;
        }

        private string QRCodeHTML(string ssURL)
        {
            string html = Resources.qrcode;
            string qrcodeLib;

            byte[] qrcodeGZ = Resources.qrcode_min_js;
            byte[] buffer = new byte[1024 * 1024];  // builtin pac gzip size: maximum 1M
            int n;

            using (GZipStream input = new GZipStream(new MemoryStream(qrcodeGZ),
                CompressionMode.Decompress, false))
            {
                n = input.Read(buffer, 0, buffer.Length);
                if (n == 0)
                {
                    throw new IOException("can not decompress qrcode lib");
                }
                qrcodeLib = System.Text.Encoding.UTF8.GetString(buffer, 0, n);
            }
            string result = html.Replace("__QRCODELIB__", qrcodeLib);
            return result.Replace("__SSURL__", ssURL);
        }

        private void QRCodeForm_Load(object sender, EventArgs e)
        {
            //QRCodeWebBrowser.DocumentText = QRCodeHTML(code);
            GenQR(code);
        }
    }
}
