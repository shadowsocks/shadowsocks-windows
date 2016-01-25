using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.View
{
    public partial class LogForm : Form
    {
        long lastOffset;
        string filename;
        Timer timer;
        const int BACK_OFFSET = 65536;
        ShadowsocksController controller;

        public LogForm(ShadowsocksController controller, string filename)
        {
            this.controller = controller;
            this.filename = filename;
            InitializeComponent();
            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            LogViewerConfig config = controller.GetConfigurationCopy().logViewer;
            if (config == null)
            {
                config = new LogViewerConfig();
            }
            else {
                topMostTrigger = config.topMost;
                wrapTextTrigger = config.wrapText;
                toolbarTrigger = config.toolbarShown;
                LogMessageTextBox.BackColor = config.GetBackgroundColor();
                LogMessageTextBox.ForeColor = config.GetTextColor();
                LogMessageTextBox.Font = config.GetFont();
            }

            UpdateTexts();
        }

        private void UpdateTexts()
        {
            FileMenuItem.Text = I18N.GetString("&File");
            OpenLocationMenuItem.Text = I18N.GetString("&Open Location");
            ExitMenuItem.Text = I18N.GetString("E&xit");
            CleanLogsButton.Text = I18N.GetString("&Clean Logs");
            ChangeFontButton.Text = I18N.GetString("Change &Font");
            WrapTextCheckBox.Text = I18N.GetString("&Wrap Text");
            TopMostCheckBox.Text = I18N.GetString("&Top Most");
            ViewMenuItem.Text = I18N.GetString("&View");
            CleanLogsMenuItem.Text = I18N.GetString("&Clean Logs");
            ChangeFontMenuItem.Text = I18N.GetString("Change &Font");
            WrapTextMenuItem.Text = I18N.GetString("&Wrap Text");
            TopMostMenuItem.Text = I18N.GetString("&Top Most");
            ShowToolbarMenuItem.Text = I18N.GetString("&Show Toolbar");
            Text = I18N.GetString("Log Viewer");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateContent();
        }

        private void InitContent()
        {
            using (StreamReader reader = new StreamReader(new FileStream(filename,
                     FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (reader.BaseStream.Length > BACK_OFFSET)
                {
                    reader.BaseStream.Seek(-BACK_OFFSET, SeekOrigin.End);
                    reader.ReadLine();
                }

                string line = "";
                while ((line = reader.ReadLine()) != null)
                    LogMessageTextBox.AppendText(line + Environment.NewLine);

                LogMessageTextBox.ScrollToCaret();

                lastOffset = reader.BaseStream.Position;
            }
        }

        private void UpdateContent()
        {
            using (StreamReader reader = new StreamReader(new FileStream(filename,
                     FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.BaseStream.Seek(lastOffset, SeekOrigin.Begin);

                string line = "";
                bool changed = false;
                while ((line = reader.ReadLine()) != null)
                {
                    changed = true;
                    LogMessageTextBox.AppendText(line + Environment.NewLine);
                }

                if (changed)
                {
                    LogMessageTextBox.ScrollToCaret();
                }

                lastOffset = reader.BaseStream.Position;
            }

            this.Text = I18N.GetString("Log Viewer") +
                $" [in: {Utils.FormatBandwidth(controller.inboundCounter)}, out: {Utils.FormatBandwidth(controller.outboundCounter)}]";
        }

        private void LogForm_Load(object sender, EventArgs e)
        {
            InitContent();

            timer = new Timer();
            timer.Interval = 300;
            timer.Tick += Timer_Tick;
            timer.Start();

            LogViewerConfig config = controller.GetConfigurationCopy().logViewer;
            if (config == null)
                config = new LogViewerConfig();
            Height = config.height;
            Width = config.width;
            Top = config.GetBestTop();
            Left = config.GetBestLeft();

            topMostTriggerLock = true;
            TopMost = TopMostMenuItem.Checked = TopMostCheckBox.Checked = topMostTrigger;
            topMostTriggerLock = false;

            wrapTextTriggerLock = true;
            LogMessageTextBox.WordWrap = WrapTextMenuItem.Checked = WrapTextCheckBox.Checked = wrapTextTrigger;
            wrapTextTriggerLock = false;

            ToolbarFlowLayoutPanel.Visible = ShowToolbarMenuItem.Checked = toolbarTrigger;
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Stop();
            LogViewerConfig config = controller.GetConfigurationCopy().logViewer;
            if (config == null)
                config = new LogViewerConfig();
            config.topMost = topMostTrigger;
            config.wrapText = wrapTextTrigger;
            config.toolbarShown = toolbarTrigger;
            config.SetFont(LogMessageTextBox.Font);
            config.SetBackgroundColor(LogMessageTextBox.BackColor);
            config.SetTextColor(LogMessageTextBox.ForeColor);
            config.top = Top;
            config.left = Left;
            config.height = Height;
            config.width = Width;
            controller.SaveLogViewerConfig(config);
        }

        private void OpenLocationMenuItem_Click(object sender, EventArgs e)
        {
            string argument = "/select, \"" + filename + "\"";
            Logging.Debug(argument);
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LogForm_Shown(object sender, EventArgs e)
        {
            LogMessageTextBox.ScrollToCaret();
        }

        #region Clean up the content in LogMessageTextBox.
        private void DoCleanLogs()
        {
            LogMessageTextBox.Clear();
        }

        private void CleanLogsMenuItem_Click(object sender, EventArgs e)
        {
            DoCleanLogs();
        }

        private void CleanLogsButton_Click(object sender, EventArgs e)
        {
            DoCleanLogs();
        }
        #endregion

        #region Change the font settings applied in LogMessageTextBox.
        private void DoChangeFont()
        {
            try
            {
                FontDialog fd = new FontDialog();
                fd.Font = LogMessageTextBox.Font;
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    LogMessageTextBox.Font = new Font(fd.Font.FontFamily, fd.Font.Size, fd.Font.Style);
                }
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void ChangeFontMenuItem_Click(object sender, EventArgs e)
        {
            DoChangeFont();
        }

        private void ChangeFontButton_Click(object sender, EventArgs e)
        {
            DoChangeFont();
        }
        #endregion

        #region Trigger the log messages to wrapable, or not.
        bool wrapTextTrigger = false;
        bool wrapTextTriggerLock = false;

        private void TriggerWrapText()
        {
            wrapTextTriggerLock = true;

            wrapTextTrigger = !wrapTextTrigger;
            LogMessageTextBox.WordWrap = wrapTextTrigger;
            LogMessageTextBox.ScrollToCaret();
            WrapTextMenuItem.Checked = WrapTextCheckBox.Checked = wrapTextTrigger;

            wrapTextTriggerLock = false;
        }

        private void WrapTextMenuItem_Click(object sender, EventArgs e)
        {
            if (!wrapTextTriggerLock)
            {
                TriggerWrapText();
            }
        }

        private void WrapTextCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!wrapTextTriggerLock)
            {
                TriggerWrapText();
            }
        }
        #endregion

        #region Trigger the window to top most, or not.
        bool topMostTrigger = false;
        bool topMostTriggerLock = false;

        private void TriggerTopMost()
        {
            topMostTriggerLock = true;

            topMostTrigger = !topMostTrigger;
            TopMost = topMostTrigger;
            TopMostMenuItem.Checked = TopMostCheckBox.Checked = topMostTrigger;

            topMostTriggerLock = false;
        }

        private void TopMostCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!topMostTriggerLock)
            {
                TriggerTopMost();
            }
        }

        private void TopMostMenuItem_Click(object sender, EventArgs e)
        {
            if (!topMostTriggerLock)
            {
                TriggerTopMost();
            }
        }
        #endregion

        private bool toolbarTrigger = false;

        private void ShowToolbarMenuItem_Click(object sender, EventArgs e)
        {
            toolbarTrigger = !toolbarTrigger;
            ToolbarFlowLayoutPanel.Visible = toolbarTrigger;
            ShowToolbarMenuItem.Checked = toolbarTrigger;
        }
    }
}
