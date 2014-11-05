using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace shadowsocks_csharp
{
    public partial class ConfigForm : Form
    {
        Local local;
        PACServer pacServer;
        Config config;
        PolipoRunner polipoRunner;

        public ConfigForm()
        {
            config = Config.Load();
            InitializeComponent();
            notifyIcon1.ContextMenu = contextMenu1;
            enableItem.Checked = config.enabled;
            configToTextBox();
        }
        
        private void showWindow()
        {
            this.Opacity = 1;
            this.Show();
        }

        private void configToTextBox()
        {
            textBox1.Text = config.server;
            textBox2.Text = config.server_port.ToString();
            textBox3.Text = config.password;
            textBox4.Text = config.local_port.ToString();
            comboBox1.Text = config.method == null ? "table" : config.method;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!config.isDefault)
            {
                this.Opacity = 0;
                reload(config); BeginInvoke(new MethodInvoker(delegate
                {
                    this.Hide();
                }));
            }
            pacServer = new PACServer();
            pacServer.Start();
            updateSystemProxy();
        }

        private void reload(Config config)
        {
            if (local != null)
            {
                local.Stop();
                if (polipoRunner != null)
                {
                    polipoRunner.Stop();
                }
            }
            if (polipoRunner == null)
            {
                polipoRunner = new PolipoRunner();
            }
            polipoRunner.Start(config);

            local = new Local(config);
            local.Start();

        }

        private void Config_Click(object sender, EventArgs e)
        {
            showWindow();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            try
            {
                Config config = new Config
                {
                    server = textBox1.Text,
                    server_port = int.Parse(textBox2.Text),
                    password = textBox3.Text,
                    local_port = int.Parse(textBox4.Text),
                    method = comboBox1.Text,
                    isDefault = false
                };
                Config.Save(config);
                this.config = config;
                reload(config);
                this.Hide();
            }
            catch (FormatException)
            {
                MessageBox.Show("there is format problem");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            configToTextBox();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (local != null) local.Stop();
            if (polipoRunner != null) polipoRunner.Stop();
            if (config.enabled)
            {
                SystemProxy.Disable();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/clowwindy/shadowsocks-csharp");
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            showWindow();
        }

        private void updateSystemProxy()
        {
            if (config.enabled)
            {
                SystemProxy.Enable();
            }
            else
            {
                SystemProxy.Disable();
            }
        }

        private void EnableItem_Click(object sender, EventArgs e)
        {
            enableItem.Checked = !enableItem.Checked;
            config.enabled = enableItem.Checked;
            Config.Save(config);
            updateSystemProxy();
        }

    }
}
