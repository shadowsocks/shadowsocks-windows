using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Shadowsocks.Util
{
    public static class ViewUtils
    {
        public static IEnumerable<TControl> GetChildControls<TControl>(this Control control) where TControl : Control
        {
            if (control.Controls.Count == 0)
            {
                return Enumerable.Empty<TControl>();
            }

            var children = control.Controls.OfType<TControl>().ToList();
            return children.SelectMany(GetChildControls<TControl>).Concat(children);
        }

        // Workaround NotifyIcon's 63 chars limit
        // https://stackoverflow.com/questions/579665/how-can-i-show-a-systray-tooltip-longer-than-63-chars
        public static void SetNotifyIconText(NotifyIcon ni, string text)
        {
            if (text.Length >= 128)
                throw new ArgumentOutOfRangeException("Text limited to 127 characters");
            Type t = typeof(NotifyIcon);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(ni, text);
            if ((bool)t.GetField("added", hidden).GetValue(ni))
                t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
        }

        public static Bitmap AddBitmapOverlay(Bitmap original, params Bitmap[] overlays)
        {
            Bitmap bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format64bppArgb);
            Graphics canvas = Graphics.FromImage(bitmap);
            canvas.DrawImage(original, new Point(0, 0));
            foreach (Bitmap overlay in overlays)
            {
                canvas.DrawImage(new Bitmap(overlay, original.Size), new Point(0, 0));
            }
            canvas.Save();
            return bitmap;
        }

        public static Bitmap ChangeBitmapColor(Bitmap original, Color colorMask)
        {
            Bitmap newBitmap = new Bitmap(original);

            for (int x = 0; x < newBitmap.Width; x++)
            {
                for (int y = 0; y < newBitmap.Height; y++)
                {
                    Color color = original.GetPixel(x, y);
                    if (color.A != 0)
                    {
                        int red = color.R * colorMask.R / 255;
                        int green = color.G * colorMask.G / 255;
                        int blue = color.B * colorMask.B / 255;
                        int alpha = color.A * colorMask.A / 255;
                        newBitmap.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                    }
                    else
                    {
                        newBitmap.SetPixel(x, y, color);
                    }
                }
            }
            return newBitmap;
        }

        public static Bitmap ResizeBitmap (Bitmap original, int width, int height)
        {
            Bitmap newBitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(original, new Rectangle(0, 0, width, height));
            }
            return newBitmap;
        }

        public static int GetScreenDpi()
        {
            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            int dpi = (int)graphics.DpiX;
            graphics.Dispose();
            return dpi;
        }
    }
}
