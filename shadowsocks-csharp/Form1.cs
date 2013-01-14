using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace shadowsocks_csharp
{
    public partial class Form1 : Form
    {
        Local local;
        Config config;

        public Form1()
        {
            config = Config.Load();
            reload(config);
            InitializeComponent();
            textBox1.Text = config.server;
            textBox2.Text = config.server_port.ToString();
            textBox3.Text = config.password;
            textBox4.Text = config.local_port.ToString();
        }

        private void reload(Config config)
        {
            if (local != null)
            {
                local.Stop();
            }
            local = new Local(config.local_port);
            local.Start();

        }

        private void Config_Click(object sender, EventArgs e)
        {

        }

        private void Quit_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            reload(Config.Load());
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            local.Stop();
        }

    }
}
