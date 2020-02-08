using Shadowsocks.View;
using System;
using System.Drawing;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace Shadowsocks.Util
{
    static class QRCodeUtil
    {
        public static string ScanScreenQRCode()
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                using (Bitmap fullImage = new Bitmap(screen.Bounds.Width, screen.Bounds.Height))
                {
                    // make screen shot
                    using (Graphics g = Graphics.FromImage(fullImage))
                    {
                        g.CopyFromScreen(screen.Bounds.X,
                                         screen.Bounds.Y,
                                         0, 0,
                                         fullImage.Size,
                                         CopyPixelOperation.SourceCopy);
                    }
                    // search qrcode
                    int maxTry = 10;
                    for (int i = 0; i < maxTry; i++)
                    {
                        int marginLeft = (int)((double)fullImage.Width * i / 2.5 / maxTry);
                        int marginTop = (int)((double)fullImage.Height * i / 2.5 / maxTry);
                        Rectangle cropRect = new Rectangle(marginLeft, marginTop, fullImage.Width - marginLeft * 2, fullImage.Height - marginTop * 2);
                        Bitmap target = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);

                        double imageScale = screen.Bounds.Width / (double)cropRect.Width;
                        using (Graphics g = Graphics.FromImage(target))
                        {
                            g.DrawImage(fullImage, new Rectangle(0, 0, target.Width, target.Height),
                                            cropRect,
                                            GraphicsUnit.Pixel);
                        }
                        BitmapLuminanceSource source = new BitmapLuminanceSource(target);
                        BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
                        QRCodeReader reader = new QRCodeReader();
                        Result result = reader.decode(bitmap);
                        // read success
                        if (result != null)
                        {
                            SplashOnQRCode(result, imageScale, marginLeft, marginTop, screen);
                            return result.Text;
                        }
                    }
                }
            }
            return null;
        }

        private static void SplashOnQRCode(Result result, double imageScale, int marginLeft, int marginTop, Screen screen)
        {
            QRCodeSplashForm splash = new QRCodeSplashForm();

            double minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
            // calculate splash position
            foreach (ResultPoint point in result.ResultPoints)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }
            minX /= imageScale;
            minY /= imageScale;
            maxX /= imageScale;
            maxY /= imageScale;
            // make it 20% larger
            double margin = (maxX - minX) * 0.20f;
            minX += -margin + marginLeft;
            maxX += margin + marginLeft;
            minY += -margin + marginTop;
            maxY += margin + marginTop;
            splash.Location = new Point(screen.Bounds.X, screen.Bounds.Y);
            // we need a panel because a window has a minimal size
            // TODO: test on high DPI
            splash.TargetRect = new Rectangle((int)minX, (int)minY, (int)maxX - (int)minX, (int)maxY - (int)minY);
            splash.Size = new Size(screen.Bounds.Width, screen.Bounds.Height);
            splash.Show();
        }

        public static (Bitmap, int) GenerateQRCode(string qrText, Size size)
        {
            QRCode code = Encoder.encode(qrText, ErrorCorrectionLevel.M);
            ByteMatrix m = code.Matrix;

            int blockSize = Math.Max(size.Height / m.Height, 1);
            int qrWidth = m.Width * blockSize;
            int qrHeight = m.Height * blockSize;

            Bitmap drawArea = new Bitmap(qrWidth, qrHeight);
            using (Graphics g = Graphics.FromImage(drawArea))
            {
                g.Clear(Color.White);
                using (Brush b = new SolidBrush(Color.Black))
                {
                    for (int row = 0; row < m.Width; row++)
                    {
                        for (int col = 0; col < m.Height; col++)
                        {
                            if (m[row, col] != 0)
                            {
                                g.FillRectangle(b, blockSize * row, blockSize * col, blockSize, blockSize);
                            }
                        }
                    }
                }
            }
            return (drawArea, blockSize);
        }
    }
}