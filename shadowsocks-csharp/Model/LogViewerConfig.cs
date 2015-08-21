using System;
using System.Drawing;
using System.Windows.Forms;

namespace Shadowsocks.Model
{
    [Serializable]
    public class LogViewerConfig
    {
        public string fontName;
        public float fontSize;
        public string bgColor;
        public string textColor;
        public bool topMost;
        public bool wrapText;
        public bool toolbarShown;
        public int width;
        public int height;
        public int top;
        public int left;

        public LogViewerConfig()
        {
            this.fontName = "Console";
            this.fontSize = 8;
            this.bgColor = "black";
            this.textColor = "white";
            this.topMost = false;
            this.wrapText = false;
            this.toolbarShown = false;
            this.width = 600;
            this.height = 400;
            this.top = (Screen.PrimaryScreen.WorkingArea.Height - height) / 2;
            this.left = (Screen.PrimaryScreen.WorkingArea.Width - width) / 2;
        }

        public Color GetBackgroundColor()
        {
            try
            {
                return ColorTranslator.FromHtml(bgColor);
            }
            catch (Exception)
            {
                return ColorTranslator.FromHtml("black");
            }
        }

        public void SetBackgroundColor(Color color)
        {
            bgColor = ColorTranslator.ToHtml(color);
        }

        public Color GetTextColor()
        {
            try
            {
                return ColorTranslator.FromHtml(textColor);
            }
            catch (Exception)
            {
                return ColorTranslator.FromHtml("white");
                throw;
            }
        }

        public void SetTextColor(Color color)
        {
            textColor = ColorTranslator.ToHtml(color);
        }
    }
}
