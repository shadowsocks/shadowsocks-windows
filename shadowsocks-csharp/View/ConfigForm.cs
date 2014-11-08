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
            IPTextBox.Focus();
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
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < modifiedConfiguration.configs.Count)
            {
                Server server = modifiedConfiguration.configs[ServersListBox.SelectedIndex];

                IPTextBox.Text = server.server;
                ServerPortTextBox.Text = server.server_port.ToString();
                PasswordTextBox.Text = server.password;
                ProxyPortTextBox.Text = server.local_port.ToString();
                EncryptionSelect.Text = server.method == null ? "aes-256-cfb" : server.method;
                ServerGroupBox.Visible = true;
                IPTextBox.Focus();
            }
            else
            {
                ServerGroupBox.Visible = false;
            }
        }

        private void loadConfiguration(Configuration configuration)
        {
            ServersListBox.Items.Clear();
            foreach (Server server in modifiedConfiguration.configs)
            {
                ServersListBox.Items.Add(string.IsNullOrEmpty(server.server) ? "New server" : server.server + ":" + server.server_port);
            }
        }

        private void loadCurrentConfiguration()
        {
            modifiedConfiguration = controller.GetConfiguration();
            loadConfiguration(modifiedConfiguration);
            oldSelectedIndex = modifiedConfiguration.index;
            ServersListBox.SelectedIndex = modifiedConfiguration.index;
            loadSelectedServer();

            updateServersMenu();
            enableItem.Checked = modifiedConfiguration.enabled;
        }

        private void updateServersMenu()
        {
            var items = ServersItem.MenuItems;

            items.Clear();

            Configuration configuration = controller.GetConfiguration();
            for (int i = 0; i < configuration.configs.Count; i++)
            {
                Server server = configuration.configs[i];
                MenuItem item = new MenuItem(server.server + ":" + server.server_port);
                item.Tag = i;
                item.Click += AServerItem_Click;
                items.Add(item);
            }
            items.Add(SeperatorItem);
            items.Add(ConfigItem);

            if (configuration.index >= 0 && configuration.index < configuration.configs.Count)
            {
                items[configuration.index].Checked = true;
            }
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
                // we are moving back to oldSelectedIndex or doing a force move
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

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!saveOldSelectedServer())
            {
                return;
            }
            Server server = Configuration.GetDefaultServer();
            modifiedConfiguration.configs.Add(server);
            loadConfiguration(modifiedConfiguration);
            ServersListBox.SelectedIndex = modifiedConfiguration.configs.Count - 1;
            oldSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            oldSelectedIndex = ServersListBox.SelectedIndex;
            if (oldSelectedIndex >= 0 && oldSelectedIndex < modifiedConfiguration.configs.Count)
            {
                modifiedConfiguration.configs.RemoveAt(oldSelectedIndex);
            }
            if (oldSelectedIndex >= modifiedConfiguration.configs.Count)
            {
                // can be -1
                oldSelectedIndex = modifiedConfiguration.configs.Count - 1;
            }
            ServersListBox.SelectedIndex = oldSelectedIndex;
            loadConfiguration(modifiedConfiguration);
            ServersListBox.SelectedIndex = oldSelectedIndex;
            loadSelectedServer();
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
            if (!saveOldSelectedServer())
            {
                return;
            }
            if (modifiedConfiguration.configs.Count == 0)
            {
                MessageBox.Show("Please add at least one server");
                return;
            }
            controller.SaveConfig(modifiedConfiguration);
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

        private void AServerItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            Configuration configuration = controller.GetConfiguration();
            configuration.index = (int)item.Tag;
            controller.SaveConfig(configuration);
        }

        private void ConfigForm_Shown(object sender, EventArgs e)
        {
            IPTextBox.Focus();
        }
    }
}
