using System;
using System.Drawing;

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

        public LogViewerConfig()
        {
            this.fontName = "Consolas";
            this.fontSize = 8;
            this.bgColor = "black";
            this.textColor = "white";
            this.topMost = false;
            this.wrapText = false;
            this.toolbarShown = false;
        }

        public Font GetFont()
        {
            try
            {
                return new Font(fontName, fontSize, FontStyle.Regular);
            }
            catch (Exception)
            {
                return new Font("Console", 8F);
            }
        }

        public void SetFont(Font font)
        {
            fontName = font.Name;
            fontSize = font.Size;
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
            }
        }

        public void SetTextColor(Color color)
        {
            textColor = ColorTranslator.ToHtml(color);
        }
    }
}
