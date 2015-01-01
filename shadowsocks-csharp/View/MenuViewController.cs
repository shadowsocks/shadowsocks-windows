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
        // and it should just do anything related to the config form
        
        private ShadowsocksController controller;
        private UpdateChecker updateChecker;

        private NotifyIcon _notifyIcon;
        private ContextMenu contextMenu1;

        private bool _isFirstRun;
        private MenuItem enableItem;
        private MenuItem AutoStartupItem;
        private MenuItem ShareOverLANItem;
        private MenuItem SeperatorItem;
        private MenuItem ConfigItem;
        private MenuItem SeperatorItem3;
        private MenuItem editPACFileItem;
        private MenuItem QRCodeItem;
        private MenuItem ShowLogItem;
        private MenuItem aboutItem;
        private MenuItem ServersItem;
        private MenuItem SeperatorItem4;
        private MenuItem quitItem;
        private MenuItem SeperatorItem2;
        private MenuItem modeItem;
        private MenuItem globalModeItem;
        private MenuItem PACModeItem;
        private ConfigForm configForm;

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();

            controller.EnableStatusChanged += controller_EnableStatusChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_PACFileReadyToOpen;
            controller.ShareOverLANStatusChanged += controller_ShareOverLANStatusChanged;
            controller.EnableGlobalChanged += controller_EnableGlobalChanged;
            controller.Errored += controller_Errored;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;
            _notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;

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

        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(), String.Format(I18N.GetString("Shadowsocks Error: {0}"), e.GetException().Message));
        }

        private void UpdateTrayIcon()
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
            bool enabled = controller.GetConfiguration().enabled;
            if (!enabled)
            {
                Bitmap iconCopy = new Bitmap(icon);
                for (int x = 0; x < iconCopy.Width; x++)
                {
                    for (int y = 0; y < iconCopy.Height; y++)
                    {
                        Color color = icon.GetPixel(x, y);
                        iconCopy.SetPixel(x, y , Color.FromArgb((byte)(color.A / 1.25), color.R, color.G, color.B));
                    }
                }
                icon = iconCopy;
            }
            _notifyIcon.Icon = Icon.FromHandle(icon.GetHicon());

            string text = I18N.GetString("Shadowsocks") + " " + UpdateChecker.Version + "\n" + (enabled ? I18N.GetString("Enabled") : I18N.GetString("Disabled")) + "\n" + controller.GetCurrentServer().FriendlyName();
            _notifyIcon.Text = text.Substring(0, Math.Min(63, text.Length));
        }

        private MenuItem CreateMenuItem(int index, string text, EventHandler click)
        {
            MenuItem result = new MenuItem(I18N.GetString(text), click);
            result.Index = index;
            return result;
        }

        private MenuItem CreateSeperatorItem(int index)
        {
            MenuItem result = new MenuItem("-");
            result.Index = index;
            return result;
        }

        private MenuItem CreateMenuGroup(int index, string text, MenuItem[] items)
        {
            MenuItem result = new MenuItem(I18N.GetString(text), items);
            result.Index = index;
            return result;
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();


            this.PACModeItem = CreateMenuItem(0, "PAC", new System.EventHandler(this.PACModeItem_Click));
            this.globalModeItem = CreateMenuItem(1, "Global", new System.EventHandler(this.GlobalModeItem_Click));

            this.SeperatorItem = CreateSeperatorItem(0);
            this.ConfigItem = CreateMenuItem(1, "Edit Servers...", new System.EventHandler(this.Config_Click));

            this.enableItem = CreateMenuItem(0, "Enable", new System.EventHandler(this.EnableItem_Click));
            this.modeItem = CreateMenuGroup(1, "Mode", new System.Windows.Forms.MenuItem[] {
            this.PACModeItem,
            this.globalModeItem});
            this.ServersItem = CreateMenuGroup(2, "Servers", new System.Windows.Forms.MenuItem[] {
            this.SeperatorItem,
            this.ConfigItem});
            this.SeperatorItem2 = CreateSeperatorItem(3);
            this.AutoStartupItem = CreateMenuItem(4, "Start on Boot", new System.EventHandler(this.AutoStartupItem_Click));
            this.ShareOverLANItem = CreateMenuItem(5, "Share over LAN", new System.EventHandler(this.ShareOverLANItem_Click));
            this.editPACFileItem = CreateMenuItem(6, "Edit PAC File...", new System.EventHandler(this.EditPACFileItem_Click));
            this.SeperatorItem3 = CreateSeperatorItem(7);
            this.QRCodeItem = CreateMenuItem(8, "Show QRCode...", new System.EventHandler(this.QRCodeItem_Click));
            this.ShowLogItem = CreateMenuItem(9, "Show Logs...", new System.EventHandler(this.ShowLogItem_Click));
            this.aboutItem = CreateMenuItem(10, "About...", new System.EventHandler(this.Config_Click));
            this.SeperatorItem4 = CreateSeperatorItem(11);
            this.quitItem = CreateMenuItem(12, "Quit", new System.EventHandler(this.Quit_Click));

            // 
            // contextMenu1
            // 
            this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.enableItem,
            this.modeItem,
            this.ServersItem,
            this.SeperatorItem2,
            this.AutoStartupItem,
            this.ShareOverLANItem,
            this.editPACFileItem,
            this.SeperatorItem3,
            this.QRCodeItem,
            this.ShowLogItem,
            this.aboutItem,
            this.SeperatorItem4,
            this.quitItem});
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void controller_EnableStatusChanged(object sender, EventArgs e)
        {
            enableItem.Checked = controller.GetConfiguration().enabled;
        }

        void controller_ShareOverLANStatusChanged(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = controller.GetConfiguration().shareOverLan;
        }

        void controller_EnableGlobalChanged(object sender, EventArgs e)
        {
            globalModeItem.Checked = controller.GetConfiguration().global;
            PACModeItem.Checked = !globalModeItem.Checked;
        }

        void controller_PACFileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        void updateChecker_NewVersionFound(object sender, EventArgs e)
        {
            _notifyIcon.BalloonTipTitle = String.Format(I18N.GetString("Shadowsocks {0} Update Found"), updateChecker.LatestVersionNumber);
            _notifyIcon.BalloonTipText = I18N.GetString("Click here to download");
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;
            _notifyIcon.ShowBalloonTip(5000);
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
            globalModeItem.Checked = config.global;
            PACModeItem.Checked = !config.global;
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
                MenuItem item = new MenuItem(server.FriendlyName());
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
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void ShowFirstTimeBalloon()
        {
            if (_isFirstRun)
            {
                _notifyIcon.BalloonTipTitle = I18N.GetString("Shadowsocks is here");
                _notifyIcon.BalloonTipText =  I18N.GetString("You can turn on/off Shadowsocks in the context menu");
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                _notifyIcon.ShowBalloonTip(0);
                _isFirstRun = false;
            }
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/shadowsocks/shadowsocks-csharp");
        }

        private void notifyIcon1_DoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowConfigForm();
            }
        }

        private void EnableItem_Click(object sender, EventArgs e)
        {
            controller.ToggleEnable(!enableItem.Checked);
        }

        private void GlobalModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleGlobal(true);
        }

        private void PACModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleGlobal(false);
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
