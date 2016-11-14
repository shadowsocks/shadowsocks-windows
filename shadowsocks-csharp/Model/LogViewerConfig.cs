using System;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Shadowsocks.Model
{
    [Serializable]
    public class LogViewerConfig
    {
        public bool topMost;
        public bool wrapText;
        public bool toolbarShown;

        public Font Font { get; set; } = new Font("Consolas", 8F);

        public Color BackgroundColor { get; set; } = Color.Black;

        public Color TextColor { get; set; } = Color.White;

        public LogViewerConfig()
        {
            topMost = false;
            wrapText = false;
            toolbarShown = false;
        }


        #region Size

        public void SaveSize()
        {
            Properties.Settings.Default.Save();
        }

        [JsonIgnore]
        public int Width
        {
            get { return Properties.Settings.Default.LogViewerWidth; }
            set { Properties.Settings.Default.LogViewerWidth = value; }
        }

        [JsonIgnore]
        public int Height
        {
            get { return Properties.Settings.Default.LogViewerHeight; }
            set { Properties.Settings.Default.LogViewerHeight = value; }
        }
        [JsonIgnore]
        public int Top
        {
            get { return Properties.Settings.Default.LogViewerTop; }
            set { Properties.Settings.Default.LogViewerTop = value; }
        }
        [JsonIgnore]
        public int Left
        {
            get { return Properties.Settings.Default.LogViewerLeft; }
            set { Properties.Settings.Default.LogViewerLeft = value; }
        }
        [JsonIgnore]
        public bool Maximized
        {
            get { return Properties.Settings.Default.LogViewerMaximized; }
            set { Properties.Settings.Default.LogViewerMaximized = value; }
        }

        [JsonIgnore]
        // Use GetBestTop() and GetBestLeft() to ensure the log viwer form can be always display IN screen. 
        public int BestLeft
        {
            get
            {
                int width = Width;
                width = (width >= 400) ? width : 400; // set up the minimum size
                return Screen.PrimaryScreen.WorkingArea.Width - width;
            }
        }

        [JsonIgnore]
        public int BestTop
        {
            get
            {
                int height = Height;
                height = (height >= 200) ? height : 200; // set up the minimum size
                return Screen.PrimaryScreen.WorkingArea.Height - height;
            }
        }

        #endregion

    }
}
