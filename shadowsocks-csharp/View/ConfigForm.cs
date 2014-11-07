using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Shadowsocks.Controller;
using Shadowsocks.Model;

namespace Shadowsocks.View
{
    public partial class ConfigForm : Form
    {
        private ShadowsocksController controller;

        // this is a copy of configuration that we are working on
        private Configuration modifiedConfiguration;
        private int oldSelectedIndex = -1;

        public ConfigForm(ShadowsocksController controller)
        {
            InitializeComponent();
            notifyIcon1.ContextMenu = contextMenu1;

            this.controller = controller;
            controller.EnableStatusChanged += controller_EnableStatusChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_PACFileReadyToOpen;

            loadCurrentConfiguration();
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            loadCurrentConfiguration();
        }

        private void controller_EnableStatusChanged(object sender, EventArgs e)
        {
            enableItem.Checked = controller.GetConfiguration().enabled;
        }

        void controller_PACFileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        
        private void showWindow()
        {
            this.Opacity = 1;
            this.Show();
        }

        private bool saveOldSelectedServer()
        {
            try
            {
                if (oldSelectedIndex == -1 || oldSelectedIndex >= modifiedConfiguration.configs.Count)
                {
                    return true;
                }
                Server server = new Server
                {
                    server = IPTextBox.Text,
                    server_port = int.Parse(ServerPortTextBox.Text),
                    password = PasswordTextBox.Text,
                    local_port = int.Parse(ProxyPortTextBox.Text),
                    method = EncryptionSelect.Text
                };
                Configuration.CheckServer(server);
                modifiedConfiguration.configs[oldSelectedIndex] = server;
                return true;
            }
            catch (FormatException)
            {
                MessageBox.Show("illegal port number format");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        private void loadSelectedServer()
        {
            Server server = modifiedConfiguration.configs[ServersListBox.SelectedIndex];

            IPTextBox.Text = server.server;
            ServerPortTextBox.Text = server.server_port.ToString();
            PasswordTextBox.Text = server.password;
            ProxyPortTextBox.Text = server.local_port.ToString();
            EncryptionSelect.Text = server.method == null ? "aes-256-cfb" : server.method;
        }

        private void loadCurrentConfiguration()
        {
            modifiedConfiguration = controller.GetConfiguration();
            
            ServersListBox.Items.Clear();
            foreach (Server server in modifiedConfiguration.configs)
            {
                ServersListBox.Items.Add(server.server);
            }
            ServersListBox.SelectedIndex = modifiedConfiguration.index;
            oldSelectedIndex = ServersListBox.SelectedIndex;

            enableItem.Checked = modifiedConfiguration.enabled;
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            if (!controller.GetConfiguration().isDefault)
            {
                this.Opacity = 0;
                BeginInvoke(new MethodInvoker(delegate
                {
                    this.Hide();
                }));
            }
        }

        private void ServersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (oldSelectedIndex == ServersListBox.SelectedIndex)
            {
                // we are moving back to oldSelectedIndex
                return;
            }
            if (!saveOldSelectedServer())
            {
                // why this won't cause stack overflow?
                ServersListBox.SelectedIndex = oldSelectedIndex;
                return;
            }
            loadSelectedServer();
            oldSelectedIndex = ServersListBox.SelectedIndex;
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
            // TODO
            Configuration config = controller.GetConfiguration();
            controller.SaveConfig(config);
            this.Hide();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            loadCurrentConfiguration();
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.Stop();
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/clowwindy/shadowsocks-csharp");
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            showWindow();
        }


        private void EnableItem_Click(object sender, EventArgs e)
        {
            enableItem.Checked = !enableItem.Checked;
            controller.ToggleEnable(enableItem.Checked);
        }

        private void EditPACFileItem_Click(object sender, EventArgs e)
        {
            controller.TouchPACFile();
        }

    }
}
