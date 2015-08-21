using Shadowsocks.Controller;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class LogForm : Form
    {
        long lastOffset;
        string filename;
        Timer timer;
        const int BACK_OFFSET = 65536;
        Model.Configuration config;

        public LogForm(string filename)
        {
            this.filename = filename;
            InitializeComponent();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            config = Model.Configuration.Load();
            if (config.logViewer == null)
            {
                config.logViewer = new Model.LogViewerConfig();
            }
            else
            {
                topMostTrigger = config.logViewer.topMost;
                wrapTextTrigger = config.logViewer.wrapText;
                toolbarTrigger = config.logViewer.toolbarShown;
                LogMessageTextBox.Font = new Font(config.logViewer.fontName, config.logViewer.fontSize);
                LogMessageTextBox.BackColor = config.logViewer.GetBackgroundColor();
                LogMessageTextBox.ForeColor = config.logViewer.GetTextColor();
            }

            UpdateTexts();
        }

        private void UpdateTexts()
        {
            FileMenuItem.Text = I18N.GetString("&File");
            OpenLocationMenuItem.Text = I18N.GetString("&Open Location");
            ExitMenuItem.Text = I18N.GetString("E&xit");
            CleanLogsButton.Text = I18N.GetString("&Clean logs");
            ChangeFontButton.Text = I18N.GetString("Change &font");
            WrapTextCheckBox.Text = I18N.GetString("&Wrap text");
            TopMostCheckBox.Text = I18N.GetString("&Top most");
            ViewMenuItem.Text = I18N.GetString("&View");
            CleanLogsMenuItem.Text = I18N.GetString("&Clean logs");
            ChangeFontMenuItem.Text = I18N.GetString("Change &font");
            WrapTextMenuItem.Text = I18N.GetString("&Wrap text");
            TopMostMenuItem.Text = I18N.GetString("&Top most");
            ShowToolbarMenuItem.Text = I18N.GetString("&Show toolbar");
            this.Text = I18N.GetString("Log Viewer");
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
        }

        private void LogForm_Load(object sender, EventArgs e)
        {
            InitContent();
            timer = new Timer();
            timer.Interval = 300;
            timer.Tick += Timer_Tick;
            timer.Start();

            this.Top = config.logViewer.top;
            this.Left = config.logViewer.left;
            this.Height = config.logViewer.height;
            this.Width = config.logViewer.width;

            topMostTriggerLock = true;
            this.TopMost = TopMostMenuItem.Checked = TopMostCheckBox.Checked = topMostTrigger;
            topMostTriggerLock = false;

            wrapTextTriggerLock = true;
            LogMessageTextBox.WordWrap = WrapTextMenuItem.Checked = WrapTextCheckBox.Checked = wrapTextTrigger;
            wrapTextTriggerLock = false;

            ToolbarFlowLayoutPanel.Visible = ShowToolbarMenuItem.Checked = toolbarTrigger;
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Stop();
            config.logViewer.topMost = topMostTrigger;
            config.logViewer.wrapText = wrapTextTrigger;
            config.logViewer.toolbarShown = toolbarTrigger;
            config.logViewer.fontName = LogMessageTextBox.Font.Name;
            config.logViewer.fontSize = LogMessageTextBox.Font.Size;
            config.logViewer.SetBackgroundColor(LogMessageTextBox.BackColor);
            config.logViewer.SetTextColor(LogMessageTextBox.ForeColor);
            config.logViewer.top = this.Top;
            config.logViewer.left = this.Left;
            config.logViewer.height = this.Height;
            config.logViewer.width = this.Width;
            Model.Configuration.Save(config);
        }

        private void OpenLocationMenuItem_Click(object sender, EventArgs e)
        {
            string argument = "/select, \"" + filename + "\"";
            Console.WriteLine(argument);
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
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
            FontDialog fd = new FontDialog();
            fd.Font = LogMessageTextBox.Font;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                LogMessageTextBox.Font = fd.Font;
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

        #region Trigger the log messages wrapable, or not.
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

        #region Trigger this window top most, or not.
        bool topMostTrigger = false;
        bool topMostTriggerLock = false;

        private void TriggerTopMost()
        {
            topMostTriggerLock = true;

            topMostTrigger = !topMostTrigger;
            this.TopMost = topMostTrigger;
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
