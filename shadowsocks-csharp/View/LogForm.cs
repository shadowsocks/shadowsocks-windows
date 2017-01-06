using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class LogForm : Form
    {
        private readonly ShadowsocksController _controller;

        private const int MaxReadSize = 65536;

        private string _currentLogFile;
        private string _currentLogFileName;
        private long _currentOffset;

        public LogForm(ShadowsocksController controller)
        {
            _controller = controller;

            InitializeComponent();

            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            UpdateTexts();
        }

        private void UpdateTexts()
        {
            fileToolStripMenuItem.Text = I18N.GetString("&File");
            clearLogToolStripMenuItem.Text = I18N.GetString("Clear &log");
            showInExplorerToolStripMenuItem.Text = I18N.GetString("Show in &Explorer");
            closeToolStripMenuItem.Text = I18N.GetString("&Close");
            viewToolStripMenuItem.Text = I18N.GetString("&View");
            fontToolStripMenuItem.Text = I18N.GetString("&Font...");
            wrapTextToolStripMenuItem.Text = I18N.GetString("&Wrap Text");
            alwaysOnTopToolStripMenuItem.Text = I18N.GetString("&Always on top");
            Text = I18N.GetString("Log Viewer");
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void showInExplorerToolStripMenuItem_Click(object sender, EventArgs _)
        {
            try
            {
                string argument = "/n" + ",/select," + Logging.LogFile;
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private void LogForm_Load(object sender, EventArgs e)
        {
            ReadLog();
        }

        private void ReadLog()
        {
            var newLogFile = Logging.LogFile;
            if (newLogFile != _currentLogFile)
            {
                _currentOffset = 0;
                _currentLogFile = newLogFile;
                _currentLogFileName = Logging.LogFileName;
            }

            try
            {
                using (
                    var reader =
                        new StreamReader(new FileStream(newLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                )
                {
                    if (_currentOffset == 0)
                    {
                        var maxSize = reader.BaseStream.Length;
                        if (maxSize > MaxReadSize)
                        {
                            reader.BaseStream.Seek(-MaxReadSize, SeekOrigin.End);
                            reader.ReadLine();
                        }
                    }
                    else
                    {
                        reader.BaseStream.Seek(_currentOffset, SeekOrigin.Begin);
                    }

                    var txt = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(txt))
                    {
                        logTextBox.AppendText(txt);
                        logTextBox.ScrollToCaret();
                    }

                    _currentOffset = reader.BaseStream.Position;
                }
            }
            catch (FileNotFoundException)
            {
            }

            Text = $@"{I18N.GetString("Log Viewer")} {_currentLogFileName}";
        }

        private void refreshTimer_Tick(object sender, EventArgs e)
        {
            ReadLog();
        }

        private void LogForm_Shown(object sender, EventArgs e)
        {
            logTextBox.ScrollToCaret();
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fontDialog = new FontDialog();
            fontDialog.Font = logTextBox.Font;
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                logTextBox.Font = fontDialog.Font;
            }
        }

        private void wrapTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wrapTextToolStripMenuItem.Checked = !wrapTextToolStripMenuItem.Checked;
        }

        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            alwaysOnTopToolStripMenuItem.Checked = !alwaysOnTopToolStripMenuItem.Checked;
        }

        private void wrapTextToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            logTextBox.WordWrap = wrapTextToolStripMenuItem.Checked;
            logTextBox.ScrollToCaret();
        }

        private void alwaysOnTopToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = alwaysOnTopToolStripMenuItem.Checked;
        }

        private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logging.Clear();
            _currentOffset = 0;
            logTextBox.Clear();
        }
    }
}
