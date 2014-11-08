using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
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
        private Configuration _modifiedConfiguration;
        private int _oldSelectedIndex = -1;
        private bool _isFirstRun;
        private bool _lockStatus;

        public ConfigForm(ShadowsocksController controller)
        {
            InitializeComponent();
            notifyIcon1.ContextMenu = contextMenu1;

            this.controller = controller;
            controller.EnableStatusChanged += controller_EnableStatusChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_PACFileReadyToOpen;

            LoadCurrentConfiguration();
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
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

        
        private void ShowWindow()
        {
            this.Opacity = 1;
            this.Show();
            IPTextBox.Focus();
            if (_lockStatus)
            {
                IPTextBox.Enabled = false;
                ServerPortTextBox.Enabled = false;
                PasswordTextBox.Enabled = false;
                EncryptionSelect.Enabled = false;
                ProxyPortTextBox.Enabled = false;
                RemarksTextBox.Enabled = false;
                AddButton.Enabled = false;
                DeleteButton.Enabled = false;
                _lockStatus = true;
                LockButton.Text = "&Unlock Settings";
            }
        }

        private bool SaveOldSelectedServer()
        {
            try
            {
                if (_oldSelectedIndex == -1 || _oldSelectedIndex >= _modifiedConfiguration.configs.Count)
                {
                    return true;
                }
                Server server = new Server
                {
                    server = IPTextBox.Text,
                    server_port = int.Parse(ServerPortTextBox.Text),
                    password = PasswordTextBox.Text,
                    local_port = int.Parse(ProxyPortTextBox.Text),
                    method = EncryptionSelect.Text,
                    remarks = RemarksTextBox.Text
                };
                Configuration.CheckServer(server);
                _modifiedConfiguration.configs[_oldSelectedIndex] = server;
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

        private void LoadSelectedServer()
        {
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < _modifiedConfiguration.configs.Count)
            {
                Server server = _modifiedConfiguration.configs[ServersListBox.SelectedIndex];

                IPTextBox.Text = server.server;
                ServerPortTextBox.Text = server.server_port.ToString();
                PasswordTextBox.Text = server.password;
                ProxyPortTextBox.Text = server.local_port.ToString();
                EncryptionSelect.Text = server.method ?? "aes-256-cfb";
                RemarksTextBox.Text = server.remarks;
                ServerGroupBox.Visible = true;
                IPTextBox.Focus();
            }
            else
            {
                ServerGroupBox.Visible = false;
            }
        }

        private void LoadConfiguration(Configuration configuration)
        {
            ServersListBox.Items.Clear();
            foreach (Server server in _modifiedConfiguration.configs)
            {
                ServersListBox.Items.Add(string.IsNullOrEmpty(server.server) ? "New server" : string.IsNullOrEmpty(server.remarks)? server.server + ":" + server.server_port : server.server + ":" + server.server_port + " (" + server.remarks + ")");
            }
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadConfiguration(_modifiedConfiguration);
            _oldSelectedIndex = _modifiedConfiguration.index;
            _lockStatus = _modifiedConfiguration.lockStatus;
            ServersListBox.SelectedIndex = _modifiedConfiguration.index;
            LoadSelectedServer();

            UpdateServersMenu();
            enableItem.Checked = _modifiedConfiguration.enabled;
        }

        private void UpdateServersMenu()
        {
            var items = ServersItem.MenuItems;

            items.Clear();

            Configuration configuration = controller.GetConfiguration();
            for (int i = 0; i < configuration.configs.Count; i++)
            {
                Server server = configuration.configs[i];
                MenuItem item = new MenuItem(string.IsNullOrEmpty(server.remarks) ? server.server + ":" + server.server_port : server.server + ":" + server.server_port + " (" + server.remarks + ")");
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
            if (controller == null || controller.GetConfiguration().isDefault)
            {
                _isFirstRun = true;
            }
            else
            {
                this.Opacity = 0;
                BeginInvoke(new MethodInvoker(delegate { this.Hide(); }));
            }
        }

        private void ServersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_oldSelectedIndex == ServersListBox.SelectedIndex)
            {
                // we are moving back to oldSelectedIndex or doing a force move
                return;
            }
            if (!SaveOldSelectedServer())
            {
                // why this won't cause stack overflow?
                ServersListBox.SelectedIndex = _oldSelectedIndex;
                return;
            }
            LoadSelectedServer();
            _oldSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            Server server = Configuration.GetDefaultServer();
            _modifiedConfiguration.configs.Add(server);
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _modifiedConfiguration.configs.Count - 1;
            _oldSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            _oldSelectedIndex = ServersListBox.SelectedIndex;
            if (_oldSelectedIndex >= 0 && _oldSelectedIndex < _modifiedConfiguration.configs.Count)
            {
                _modifiedConfiguration.configs.RemoveAt(_oldSelectedIndex);
            }
            if (_oldSelectedIndex >= _modifiedConfiguration.configs.Count)
            {
                // can be -1
                _oldSelectedIndex = _modifiedConfiguration.configs.Count - 1;
            }
            ServersListBox.SelectedIndex = _oldSelectedIndex;
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _oldSelectedIndex;
            LoadSelectedServer();
        }

        private void Config_Click(object sender, EventArgs e)
        {
            ShowWindow();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ShowFirstTimeBalloon()
        {
            if (_isFirstRun)
            {
                notifyIcon1.BalloonTipTitle = "Shadowsocks is here";
                notifyIcon1.BalloonTipText = "You can turn on/off Shadowsocks in the context menu";
                notifyIcon1.ShowBalloonTip(0);
                _isFirstRun = false;
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            if (_modifiedConfiguration.configs.Count == 0)
            {
                MessageBox.Show("Please add at least one server");
                return;
            }
            _modifiedConfiguration.lockStatus = _lockStatus;
            controller.SaveConfig(_modifiedConfiguration);
            this.Hide();
            ShowFirstTimeBalloon();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            LoadCurrentConfiguration();
            ShowFirstTimeBalloon();
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
            ShowWindow();
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

        private void QRCodeItem_Click(object sender, EventArgs e)
        {
            new QRCodeForm(controller.GetQRCodeForCurrentServer()).Show();
        }

        private void ChangeLockStatus(bool lockStatus)
        {
            IPTextBox.Enabled = lockStatus;
            ServerPortTextBox.Enabled = lockStatus;
            PasswordTextBox.Enabled = lockStatus;
            EncryptionSelect.Enabled = lockStatus;
            ProxyPortTextBox.Enabled = lockStatus;
            RemarksTextBox.Enabled = lockStatus;
            AddButton.Enabled = lockStatus;
            DeleteButton.Enabled = lockStatus;
            _lockStatus = !lockStatus;
            if (lockStatus)
            {
                LockButton.Text = "&Lock";
            }
            else
            {
                LockButton.Text = "&Unlock";
            }
        }

        private void LockButton_Click(object sender, EventArgs e)
        {
            ChangeLockStatus(_lockStatus);
        }
    }
}
