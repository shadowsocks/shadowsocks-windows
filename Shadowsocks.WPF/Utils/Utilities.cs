using Shadowsocks.WPF.Models;
using Splat;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;

namespace Shadowsocks.WPF.Utils
{
    public static class Utilities
    {
        private static string _tempPath = null!;

        public static readonly string ExecutablePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        public static readonly string WorkingDirectory = Path.GetDirectoryName(ExecutablePath) ?? "";

        // return path to store temporary files
        public static string GetTempPath()
        {
            if (_tempPath == null)
            {
                bool isPortableMode = false; // TODO: fix --profile-directory
                try
                {
                    if (isPortableMode)
                    {
                        _tempPath = Directory.CreateDirectory("ss_win_temp").FullName;
                        // don't use "/", it will fail when we call explorer /select xxx/ss_win_temp\xxx.log
                    }
                    else
                    {
                        _tempPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), @"Shadowsocks\ss_win_temp_" + ExecutablePath?.GetHashCode())).FullName;
                    }
                }
                catch (Exception e)
                {
                    LogHost.Default.Error(e, "Error: failed to create temporary directory.");
                    throw;
                }
            }
            return _tempPath;
        }

        // return a full path with filename combined which pointed to the temporary directory
        public static string GetTempPath(string filename) => Path.Combine(GetTempPath(), filename);

        public static string ScanQRCodeFromScreen()
        {
            var screenLeft = SystemParameters.VirtualScreenLeft;
            var screenTop = SystemParameters.VirtualScreenTop;
            var screenWidth = SystemParameters.VirtualScreenWidth;
            var screenHeight = SystemParameters.VirtualScreenHeight;

            using (Bitmap bmp = new Bitmap((int)screenWidth, (int)screenHeight))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                    g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                int maxTry = 10;
                for (int i = 0; i < maxTry; i++)
                {
                    int marginLeft = (int)((double)bmp.Width * i / 2.5 / maxTry);
                    int marginTop = (int)((double)bmp.Height * i / 2.5 / maxTry);
                    Rectangle cropRect = new Rectangle(marginLeft, marginTop, bmp.Width - marginLeft * 2, bmp.Height - marginTop * 2);
                    Bitmap target = new Bitmap((int)screenWidth, (int)screenHeight);

                    double imageScale = screenWidth / cropRect.Width;
                    using (Graphics g = Graphics.FromImage(target))
                        g.DrawImage(bmp, new Rectangle(0, 0, target.Width, target.Height), cropRect, GraphicsUnit.Pixel);
                    var source = new BitmapLuminanceSource(target);
                    var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                    QRCodeReader reader = new QRCodeReader();
                    var result = reader.decode(bitmap);
                    if (result != null)
                        return result.Text;
                }
            }
            return "";
        }

        public static void OpenInBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
