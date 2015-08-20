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

        public LogForm(string filename)
        {
            this.filename = filename;
            InitializeComponent();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            UpdateTexts();
        }

        private void UpdateTexts()
        {
            FileMenuItem.Text = I18N.GetString("&File");
            OpenLocationMenuItem.Text = I18N.GetString("&Open Location");
            ExitMenuItem.Text = I18N.GetString("E&xit");
            CleanLogsButton.Text = I18N.GetString("&Clean logs");
            ChangeFontButton.Text = I18N.GetString("&Font");
            WrapTextCheckBox.Text = I18N.GetString("&Wrap text");
            TopMostCheckBox.Text = I18N.GetString("&Top most");
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
                    LogMessageTextBox.AppendText(line + "\r\n");

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
                    LogMessageTextBox.AppendText(line + "\r\n");
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
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Stop();
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

        private void WrapTextCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            LogMessageTextBox.WordWrap = WrapTextCheckBox.Checked;
            LogMessageTextBox.ScrollToCaret();
        }

        private void CleanLogsButton_Click(object sender, EventArgs e)
        {
            LogMessageTextBox.Clear();
        }

        private void ChangeFontButton_Click(object sender, EventArgs e)
        {
            FontDialog fd = new FontDialog();
            fd.Font = LogMessageTextBox.Font;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                LogMessageTextBox.Font = fd.Font;
            }
        }

        private void TopMostCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = TopMostCheckBox.Checked;
        }
    }
}
