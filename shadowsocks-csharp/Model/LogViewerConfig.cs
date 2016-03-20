using Shadowsocks.View;
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
            fontName = "Consolas";
            fontSize = 8;
            bgColor = "black";
            textColor = "white";
            topMost = false;
            wrapText = false;
            toolbarShown = false;
            width = 600;
            height = 400;
            left = GetBestLeft();
            top = GetBestTop();
        }

        // Use GetBestTop() and GetBestLeft() to ensure the log viwer form can be always display IN screen. 
        public int GetBestLeft()
        {
            width = (width >= 400) ? width : 400;  // set up the minimum size
            return Screen.PrimaryScreen.WorkingArea.Width - width;
        }

        public int GetBestTop()
        {
            height = (height >= 200) ? height : 200;  // set up the minimum size
            return Screen.PrimaryScreen.WorkingArea.Height - height;
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
