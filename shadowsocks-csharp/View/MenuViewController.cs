using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public class MenuViewController
    {
        // yes this is just a menu view controller
        // when config form is closed, it moves away from RAM
        // and it should just does anything related to the config form
        
        private ShadowsocksController controller;
        private UpdateChecker updateChecker;

        private NotifyIcon notifyIcon1;
        private ContextMenu contextMenu1;

        private bool _isFirstRun;
        private MenuItem enableItem;
        private MenuItem AutoStartupItem;
        private MenuItem ShareOverLANItem;
        private MenuItem SeperatorItem;
        private MenuItem ConfigItem;
        private MenuItem menuItem4;
        private MenuItem editPACFileItem;
        private MenuItem QRCodeItem;
        private MenuItem ShowLogItem;
        private MenuItem aboutItem;
        private MenuItem ServersItem;
        private MenuItem menuItem3;
        private MenuItem quitItem;
        private MenuItem menuItem1;
        private ConfigForm configForm;

        public MenuViewController(ShadowsocksController controller)
        {
            LoadMenu();
            LoadTrayIcon();

            this.controller = controller;
            controller.EnableStatusChanged += controller_EnableStatusChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_PACFileReadyToOpen;
            controller.ShareOverLANStatusChanged += controller_ShareOverLANStatusChanged;

            this.updateChecker = new UpdateChecker();
            updateChecker.NewVersionFound += updateChecker_NewVersionFound;

            LoadCurrentConfiguration();

            updateChecker.CheckUpdate();

            if (controller.GetConfiguration().isDefault)
            {
                _isFirstRun = true;
                ShowConfigForm();
            }
        }

        private void LoadTrayIcon()
        {
            int dpi;
            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            dpi = (int)graphics.DpiX;
            graphics.Dispose();
            Bitmap icon = null;
            if (dpi < 97)
            {
                // dpi = 96;
                icon = Resources.ss16;
            }
            else if (dpi < 121)
            {
                // dpi = 120;
                icon = Resources.ss20;
            }
            else
            {
                icon = Resources.ss24;
            }
            notifyIcon1 = new NotifyIcon();
            notifyIcon1.Icon = Icon.FromHandle(icon.GetHicon());
            notifyIcon1.Visible = true;

            notifyIcon1.ContextMenu = contextMenu1;
            notifyIcon1.DoubleClick +=notifyIcon1_DoubleClick;
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.enableItem = new System.Windows.Forms.MenuItem();
            this.AutoStartupItem = new System.Windows.Forms.MenuItem();
            this.ShareOverLANItem = new System.Windows.Forms.MenuItem();
            this.ServersItem = new System.Windows.Forms.MenuItem();
            this.SeperatorItem = new System.Windows.Forms.MenuItem();
            this.ConfigItem = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.editPACFileItem = new System.Windows.Forms.MenuItem();
            this.QRCodeItem = new System.Windows.Forms.MenuItem();
            this.ShowLogItem = new System.Windows.Forms.MenuItem();
            this.aboutItem = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.quitItem = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();

            // 
            // contextMenu1
            // 
            this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.enableItem,
            this.ServersItem,
            this.menuItem1,
            this.AutoStartupItem,
            this.ShareOverLANItem,
            this.editPACFileItem,
            this.menuItem4,
            this.QRCodeItem,
            this.ShowLogItem,
            this.aboutItem,
            this.menuItem3,
            this.quitItem});
            // 
            // enableItem
            // 
            this.enableItem.Index = 0;
            this.enableItem.Text = "&Enable";
            this.enableItem.Click += new System.EventHandler(this.EnableItem_Click);
            // 
            // AutoStartupItem
            // 
            this.AutoStartupItem.Index = 3;
            this.AutoStartupItem.Text = "Start on Boot";
            this.AutoStartupItem.Click += new System.EventHandler(this.AutoStartupItem_Click);
            // 
            // ShareOverLANItem
            // 
            this.ShareOverLANItem.Index = 4;
            this.ShareOverLANItem.Text = "Share over LAN";
            this.ShareOverLANItem.Click += new System.EventHandler(this.ShareOverLANItem_Click);
            // 
            // ServersItem
            // 
            this.ServersItem.Index = 1;
            this.ServersItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.SeperatorItem,
            this.ConfigItem});
            this.ServersItem.Text = "&Servers";
            // 
            // SeperatorItem
            // 
            this.SeperatorItem.Index = 0;
            this.SeperatorItem.Text = "-";
            // 
            // ConfigItem
            // 
            this.ConfigItem.Index = 1;
            this.ConfigItem.Text = "Edit Servers...";
            this.ConfigItem.Click += new System.EventHandler(this.Config_Click);
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 6;
            this.menuItem4.Text = "-";
            // 
            // editPACFileItem
            // 
            this.editPACFileItem.Index = 5;
            this.editPACFileItem.Text = "Edit &PAC File...";
            this.editPACFileItem.Click += new System.EventHandler(this.EditPACFileItem_Click);
            // 
            // QRCodeItem
            // 
            this.QRCodeItem.Index = 7;
            this.QRCodeItem.Text = "Show &QRCode...";
            this.QRCodeItem.Click += new System.EventHandler(this.QRCodeItem_Click);
            // 
            // ShowLogItem
            // 
            this.ShowLogItem.Index = 8;
            this.ShowLogItem.Text = "Show Logs...";
            this.ShowLogItem.Click += new System.EventHandler(this.ShowLogItem_Click);
            // 
            // aboutItem
            // 
            this.aboutItem.Index = 9;
            this.aboutItem.Text = "About...";
            this.aboutItem.Click += new System.EventHandler(this.AboutItem_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 10;
            this.menuItem3.Text = "-";
            // 
            // quitItem
            // 
            this.quitItem.Index = 11;
            this.quitItem.Text = "&Quit";
            this.quitItem.Click += new System.EventHandler(this.Quit_Click);
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 2;
            this.menuItem1.Text = "-";
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void controller_EnableStatusChanged(object sender, EventArgs e)
        {
            enableItem.Checked = controller.GetConfiguration().enabled;
        }

        void controller_ShareOverLANStatusChanged(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = controller.GetConfiguration().shareOverLan;
        }

        void controller_PACFileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        void updateChecker_NewVersionFound(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "Shadowsocks " + updateChecker.LatestVersionNumber + " Update Found";
            notifyIcon1.BalloonTipText = "Click here to download";
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipClicked += notifyIcon1_BalloonTipClicked;
            notifyIcon1.ShowBalloonTip(5000);
            _isFirstRun = false;
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(updateChecker.LatestVersionURL);
        }


        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetConfiguration();
            UpdateServersMenu();
            enableItem.Checked = config.enabled;
            ShareOverLANItem.Checked = config.shareOverLan;
            AutoStartupItem.Checked = AutoStartup.Check();
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

        private void ShowConfigForm()
        {
            if (configForm != null)
            {
                configForm.Focus();
            }
            else
            {
                configForm = new ConfigForm(controller);
                configForm.Show();
                configForm.FormClosed += configForm_FormClosed;
            }
        }

        void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            configForm = null;
            Util.Util.ReleaseMemory();
            ShowFirstTimeBalloon();
        }

        private void Config_Click(object sender, EventArgs e)
        {
            ShowConfigForm();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            controller.Stop();
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        private void ShowFirstTimeBalloon()
        {
            if (_isFirstRun)
            {
                notifyIcon1.BalloonTipTitle = "Shadowsocks is here";
                notifyIcon1.BalloonTipText = "You can turn on/off Shadowsocks in the context menu";
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon1.ShowBalloonTip(0);
                _isFirstRun = false;
            }
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/clowwindy/shadowsocks-csharp");
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            ShowConfigForm();
        }


        private void EnableItem_Click(object sender, EventArgs e)
        {
            enableItem.Checked = !enableItem.Checked;
            controller.ToggleEnable(enableItem.Checked);
        }

        private void ShareOverLANItem_Click(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = !ShareOverLANItem.Checked;
            controller.ToggleShareOverLAN(ShareOverLANItem.Checked);
        }

        private void EditPACFileItem_Click(object sender, EventArgs e)
        {
            controller.TouchPACFile();
        }

        private void AServerItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectServerIndex((int)item.Tag);
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            string argument = Logging.LogFile;

            System.Diagnostics.Process.Start("notepad.exe", argument);
        }

        private void QRCodeItem_Click(object sender, EventArgs e)
        {
            QRCodeForm qrCodeForm = new QRCodeForm(controller.GetQRCodeForCurrentServer());
            //qrCodeForm.Icon = this.Icon;
            // TODO
            qrCodeForm.Show();
        }

		private void AutoStartupItem_Click(object sender, EventArgs e) {
			AutoStartupItem.Checked = !AutoStartupItem.Checked;
			if (!AutoStartup.Set(AutoStartupItem.Checked)) {
				MessageBox.Show("Failed to edit registry");
			}
		}
    }
}
