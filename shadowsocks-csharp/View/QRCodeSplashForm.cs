using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace Shadowsocks.View
{
    public class QRCodeSplashForm : PerPixelAlphaForm
    {
        public Rectangle TargetRect;

        public QRCodeSplashForm()
        {
            this.Load += QRCodeSplashForm_Load;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1, 1);
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "QRCodeSplashForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
        }

        private Timer timer;
        private int flashStep;
        private static double FPS = 1.0 / 15 * 1000; // System.Windows.Forms.Timer resolution is 15ms
        private static double ANIMATION_TIME = 0.5;
        private static int ANIMATION_STEPS = (int)(ANIMATION_TIME * FPS);
        Stopwatch sw;
        int x;
        int y;
        int w;
        int h;
        Bitmap bitmap;
        Graphics g;
        Pen pen;
        SolidBrush brush;

        private void QRCodeSplashForm_Load(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            flashStep = 0;
            x = 0;
            y = 0;
            w = Width;
            h = Height;
            sw = Stopwatch.StartNew();
            timer = new Timer();
            timer.Interval = (int)(ANIMATION_TIME * 1000 / ANIMATION_STEPS);
            timer.Tick += timer_Tick;
            timer.Start();
            bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(bitmap);
            pen = new Pen(Color.Red, 3);
            brush = new SolidBrush(Color.FromArgb(30, Color.Red));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000; // This form has to have the WS_EX_LAYERED extended style
                return cp;
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            double percent = (double)sw.ElapsedMilliseconds / 1000.0 / (double)ANIMATION_TIME;
            if (percent < 1)
            {
                // ease out
                percent = 1 - Math.Pow((1 - percent), 4);
                x = (int)(TargetRect.X * percent);
                y = (int)(TargetRect.Y * percent);
                w = (int)(TargetRect.Width * percent + this.Size.Width * (1 - percent));
                h = (int)(TargetRect.Height * percent + this.Size.Height * (1 - percent));
                //codeRectView.Location = new Point(x, y);
                //codeRectView.Size = new Size(w, h);
                pen.Color = Color.FromArgb((int)(255 * percent), Color.Red);
                brush.Color = Color.FromArgb((int)(30 * percent), Color.Red);
                g.Clear(Color.Transparent);
                g.FillRectangle(brush, x, y, w, h);
                g.DrawRectangle(pen, x, y, w, h);
                SetBitmap(bitmap);
            }
            else
            {
                if (flashStep == 0)
                {
                    timer.Interval = 100;
                    g.Clear(Color.Transparent);
                    SetBitmap(bitmap);
                }
                else if (flashStep == 1)
                {
                    timer.Interval = 50;
                    g.FillRectangle(brush, x, y, w, h);
                    g.DrawRectangle(pen, x, y, w, h);
                    SetBitmap(bitmap);
                }
                else if (flashStep == 1)
                {
                    g.Clear(Color.Transparent);
                    SetBitmap(bitmap);
                }
                else if (flashStep == 2)
                {
                    g.FillRectangle(brush, x, y, w, h);
                    g.DrawRectangle(pen, x, y, w, h);
                    SetBitmap(bitmap);
                }
                else if (flashStep == 3)
                {
                    g.Clear(Color.Transparent);
                    SetBitmap(bitmap);
                }
                else if (flashStep == 4)
                {
                    g.FillRectangle(brush, x, y, w, h);
                    g.DrawRectangle(pen, x, y, w, h);
                    SetBitmap(bitmap);
                }
                else
                {
                    sw.Stop();
                    timer.Stop();
                    pen.Dispose();
                    brush.Dispose();
                    bitmap.Dispose();
                    this.Close();
                }
                flashStep++;
            }
        }
    }


    // class that exposes needed win32 gdi functions.
    class Win32
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public Int32 x;
            public Int32 y;

            public Point(Int32 x, Int32 y) { this.x = x; this.y = y; }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Size
        {
            public Int32 cx;
            public Int32 cy;

            public Size(Int32 cx, Int32 cy) { this.cx = cx; this.cy = cy; }
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ARGB
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }


        public const Int32 ULW_COLORKEY = 0x00000001;
        public const Int32 ULW_ALPHA = 0x00000002;
        public const Int32 ULW_OPAQUE = 0x00000004;

        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;


        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pprSrc, Int32 crKey, ref BLENDFUNCTION pblend, Int32 dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int DeleteObject(IntPtr hObject);
    }


    public class PerPixelAlphaForm : Form
    {
        // http://www.codeproject.com/Articles/1822/Per-Pixel-Alpha-Blend-in-C
        // Rui Lopes

        public PerPixelAlphaForm()
        {
            // This form should not have a border or else Windows will clip it.
            FormBorderStyle = FormBorderStyle.None;
        }

        public void SetBitmap(Bitmap bitmap)
        {
            SetBitmap(bitmap, 255);
        }


        /// <para>Changes the current bitmap with a custom opacity level.  Here is where all happens!</para>
        public void SetBitmap(Bitmap bitmap, byte opacity)
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                throw new ApplicationException("The bitmap must be 32ppp with alpha-channel.");

            // The idea of this is very simple,
            // 1. Create a compatible DC with screen;
            // 2. Select the bitmap with 32bpp with alpha-channel in the compatible DC;
            // 3. Call the UpdateLayeredWindow.

            IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
            IntPtr memDc = Win32.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));  // grab a GDI handle from this GDI+ bitmap
                oldBitmap = Win32.SelectObject(memDc, hBitmap);

                Win32.Size size = new Win32.Size(bitmap.Width, bitmap.Height);
                Win32.Point pointSource = new Win32.Point(0, 0);
                Win32.Point topPos = new Win32.Point(Left, Top);
                Win32.BLENDFUNCTION blend = new Win32.BLENDFUNCTION();
                blend.BlendOp = Win32.AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = opacity;
                blend.AlphaFormat = Win32.AC_SRC_ALPHA;

                Win32.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, Win32.ULW_ALPHA);
            }
            finally
            {
                Win32.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    Win32.SelectObject(memDc, oldBitmap);
                    //Windows.DeleteObject(hBitmap); // The documentation says that we have to use the Windows.DeleteObject... but since there is no such method I use the normal DeleteObject from Win32 GDI and it's working fine without any resource leak.
                    Win32.DeleteObject(hBitmap);
                }
                Win32.DeleteDC(memDc);
            }
        }


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000; // This form has to have the WS_EX_LAYERED extended style
                return cp;
            }
        }
    }
}
