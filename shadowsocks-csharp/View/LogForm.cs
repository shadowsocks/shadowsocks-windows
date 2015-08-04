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
        FileSystemWatcher watcher;
        long lastOffset;
        string filename;
        Timer timer;
        const int BACK_OFFSET = 65536;

        public LogForm(string filename)
        {
            this.filename = filename;
            InitializeComponent();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
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
                    textBox1.AppendText(line + "\r\n");

                textBox1.ScrollToCaret();

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
                    textBox1.AppendText(line + "\r\n");
                }

                if (changed)
                {
                    textBox1.ScrollToCaret();
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

        private void menuItem2_Click(object sender, EventArgs e)
        {
            string argument = @"/select, " + filename;
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void menuItem3_Click(object sender, EventArgs e)
        {

        }

        private void menuItem4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LogForm_Shown(object sender, EventArgs e)
        {
            textBox1.ScrollToCaret();
        }
    }
}
