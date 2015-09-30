using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

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
        private bool _isStartupChecking;
        private MenuItem enableItem;
        private MenuItem modeItem;
        private MenuItem AutoStartupItem;
        private MenuItem ShareOverLANItem;
        private MenuItem SeperatorItem;
        private MenuItem ConfigItem;
        private MenuItem ServersItem;
        private MenuItem globalModeItem;
        private MenuItem PACModeItem;
        private MenuItem localPACItem;
        private MenuItem onlinePACItem;
        private MenuItem editLocalPACItem;
        private MenuItem updateFromGFWListItem;
        private MenuItem editGFWUserRuleItem;
        private MenuItem editOnlinePACItem;
        private MenuItem autoCheckUpdatesToggleItem;
        private ConfigForm configForm;
        private string _urlToOpen;

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();

            controller.EnableStatusChanged += controller_EnableStatusChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_FileReadyToOpen;
            controller.UserRuleFileReadyToOpen += controller_FileReadyToOpen;
            controller.ShareOverLANStatusChanged += controller_ShareOverLANStatusChanged;
            controller.EnableGlobalChanged += controller_EnableGlobalChanged;
            controller.Errored += controller_Errored;
            controller.UpdatePACFromGFWListCompleted += controller_UpdatePACFromGFWListCompleted;
            controller.UpdatePACFromGFWListError += controller_UpdatePACFromGFWListError;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;
            _notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;

            this.updateChecker = new UpdateChecker();
            updateChecker.CheckUpdateCompleted += updateChecker_CheckUpdateCompleted;

            LoadCurrentConfiguration();

            Configuration config = controller.GetConfigurationCopy();

            if (config.autoCheckUpdate)
            {
                _isStartupChecking = true;
                updateChecker.CheckUpdate(config);
            }

            if (config.isDefault)
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
            Configuration config = controller.GetConfigurationCopy();
            bool enabled = config.enabled;
            bool global = config.global;
            if (!enabled)
            {
                Bitmap iconCopy = new Bitmap(icon);
                for (int x = 0; x < iconCopy.Width; x++)
                {
                    for (int y = 0; y < iconCopy.Height; y++)
                    {
                        Color color = icon.GetPixel(x, y);
                        iconCopy.SetPixel(x, y, Color.FromArgb((byte)(color.A / 1.25), color.R, color.G, color.B));
                    }
                }
                icon = iconCopy;
            }
            _notifyIcon.Icon = Icon.FromHandle(icon.GetHicon());

            string serverInfo = null;
            if (controller.GetCurrentStrategy() != null)
            {
                serverInfo = controller.GetCurrentStrategy().Name;
            }
            else
            {
                serverInfo = config.GetCurrentServer().FriendlyName();
            }
            // we want to show more details but notify icon title is limited to 63 characters
            string text = I18N.GetString("Shadowsocks") + " " + UpdateChecker.Version + "\n" +
                (enabled ?
                    I18N.GetString("System Proxy On: ") + (global ? I18N.GetString("Global") : I18N.GetString("PAC")) :
                    String.Format(I18N.GetString("Running: Port {0}"), config.localPort))  // this feedback is very important because they need to know Shadowsocks is running
                + "\n" + serverInfo;
            _notifyIcon.Text = text.Substring(0, Math.Min(63, text.Length));
        }

        private MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(I18N.GetString(text), click);
        }

        private MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(I18N.GetString(text), items);
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new ContextMenu(new MenuItem[] {
                this.enableItem = CreateMenuItem("Enable System Proxy", new EventHandler(this.EnableItem_Click)),
                this.modeItem = CreateMenuGroup("Mode", new MenuItem[] {
                    this.PACModeItem = CreateMenuItem("PAC", new EventHandler(this.PACModeItem_Click)),
                    this.globalModeItem = CreateMenuItem("Global", new EventHandler(this.GlobalModeItem_Click))
                }),
                this.ServersItem = CreateMenuGroup("Servers", new MenuItem[] {
                    this.SeperatorItem = new MenuItem("-"),
                    this.ConfigItem = CreateMenuItem("Edit Servers...", new EventHandler(this.Config_Click)),
                    CreateMenuItem("Statistics Config...", StatisticsConfigItem_Click),
                    CreateMenuItem("Show QRCode...", new EventHandler(this.QRCodeItem_Click)),
                    CreateMenuItem("Scan QRCode from Screen...", new EventHandler(this.ScanQRCodeItem_Click))
                }),
                CreateMenuGroup("PAC ", new MenuItem[] {
                    this.localPACItem = CreateMenuItem("Local PAC", new EventHandler(this.LocalPACItem_Click)),
                    this.onlinePACItem = CreateMenuItem("Online PAC", new EventHandler(this.OnlinePACItem_Click)),
                    new MenuItem("-"),
                    this.editLocalPACItem = CreateMenuItem("Edit Local PAC File...", new EventHandler(this.EditPACFileItem_Click)),
                    this.updateFromGFWListItem = CreateMenuItem("Update Local PAC from GFWList", new EventHandler(this.UpdatePACFromGFWListItem_Click)),
                    this.editGFWUserRuleItem = CreateMenuItem("Edit User Rule for GFWList...", new EventHandler(this.EditUserRuleFileForGFWListItem_Click)),
                    this.editOnlinePACItem = CreateMenuItem("Edit Online PAC URL...", new EventHandler(this.UpdateOnlinePACURLItem_Click)),
                }),
                new MenuItem("-"),
                this.AutoStartupItem = CreateMenuItem("Start on Boot", new EventHandler(this.AutoStartupItem_Click)),
                this.ShareOverLANItem = CreateMenuItem("Allow Clients from LAN", new EventHandler(this.ShareOverLANItem_Click)),
                new MenuItem("-"),
                CreateMenuItem("Show Logs...", new EventHandler(this.ShowLogItem_Click)),
                CreateMenuGroup("Updates...", new MenuItem[] {
                    CreateMenuItem("Check for Updates...", new EventHandler(this.checkUpdatesItem_Click)),
                    new MenuItem("-"),
                    this.autoCheckUpdatesToggleItem = CreateMenuItem("Check for Updates at Startup", new EventHandler(this.autoCheckUpdatesToggleItem_Click)),
                }),
                CreateMenuItem("About...", new EventHandler(this.AboutItem_Click)),
                new MenuItem("-"),
                CreateMenuItem("Quit", new EventHandler(this.Quit_Click))
            });
        }


        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void controller_EnableStatusChanged(object sender, EventArgs e)
        {
            enableItem.Checked = controller.GetConfigurationCopy().enabled;
            modeItem.Enabled = enableItem.Checked;
        }

        void controller_ShareOverLANStatusChanged(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = controller.GetConfigurationCopy().shareOverLan;
        }

        void controller_EnableGlobalChanged(object sender, EventArgs e)
        {
            globalModeItem.Checked = controller.GetConfigurationCopy().global;
            PACModeItem.Checked = !globalModeItem.Checked;
        }

        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
        }

        void controller_UpdatePACFromGFWListError(object sender, System.IO.ErrorEventArgs e)
        {
            ShowBalloonTip(I18N.GetString("Failed to update PAC file"), e.GetException().Message, ToolTipIcon.Error, 5000);
            Logging.LogUsefulException(e.GetException());
        }

        void controller_UpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            string result = e.Success ? I18N.GetString("PAC updated") : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
            ShowBalloonTip(I18N.GetString("Shadowsocks"), result, ToolTipIcon.Info, 1000);
        }

        void updateChecker_CheckUpdateCompleted(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                ShowBalloonTip(String.Format(I18N.GetString("Shadowsocks {0} Update Found"), updateChecker.LatestVersionNumber), I18N.GetString("Click here to update"), ToolTipIcon.Info, 5000);
                _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;
                _isFirstRun = false;
            }
            else if (!_isStartupChecking)
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("No update is available"), ToolTipIcon.Info, 5000);
                _isFirstRun = false;
            }
            _isStartupChecking = false;
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            _notifyIcon.BalloonTipClicked -= notifyIcon1_BalloonTipClicked;
            string argument = "/select, \"" + updateChecker.LatestVersionLocalName + "\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }


        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetConfigurationCopy();
            UpdateServersMenu();
            enableItem.Checked = config.enabled;
            modeItem.Enabled = config.enabled;
            globalModeItem.Checked = config.global;
            PACModeItem.Checked = !config.global;
            ShareOverLANItem.Checked = config.shareOverLan;
            AutoStartupItem.Checked = AutoStartup.Check();
            onlinePACItem.Checked = onlinePACItem.Enabled && config.useOnlinePac;
            localPACItem.Checked = !onlinePACItem.Checked;
            UpdatePACItemsEnabledStatus();
            UpdateUpdateMenu();
        }

        private void UpdateServersMenu()
        {
            var items = ServersItem.MenuItems;
            while (items[0] != SeperatorItem)
            {
                items.RemoveAt(0);
            }
            int i = 0;
            foreach (var strategy in controller.GetStrategies())
            {
                MenuItem item = new MenuItem(strategy.Name);
                item.Tag = strategy.ID;
                item.Click += AStrategyItem_Click;
                items.Add(i, item);
                i++;
            }
            int strategyCount = i;
            Configuration configuration = controller.GetConfigurationCopy();
            foreach (var server in configuration.configs)
            {
                MenuItem item = new MenuItem(server.FriendlyName());
                item.Tag = i - strategyCount;
                item.Click += AServerItem_Click;
                items.Add(i, item);
                i++;
            }

            foreach (MenuItem item in items)
            {
                if (item.Tag != null && (item.Tag.ToString() == configuration.index.ToString() || item.Tag.ToString() == configuration.strategy))
                {
                    item.Checked = true;
                }

            }
        }

        private void ShowConfigForm()
        {
            if (configForm != null)
            {
                configForm.Activate();
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
            Util.Utils.ReleaseMemory(true);
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
                _notifyIcon.BalloonTipText = I18N.GetString("You can turn on/off Shadowsocks in the context menu");
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                _notifyIcon.ShowBalloonTip(0);
                _isFirstRun = false;
            }
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/shadowsocks/shadowsocks-windows");
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

        private void UpdatePACFromGFWListItem_Click(object sender, EventArgs e)
        {
            controller.UpdatePACFromGFWList();
        }

        private void EditUserRuleFileForGFWListItem_Click(object sender, EventArgs e)
        {
            controller.TouchUserRuleFile();
        }

        private void AServerItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectServerIndex((int)item.Tag);
        }

        private void AStrategyItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectStrategy((string)item.Tag);
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            string argument = Logging.LogFile;

            new LogForm(controller, argument).Show();
        }
        
        private void StatisticsConfigItem_Click(object sender, EventArgs e)
        {
            StatisticsStrategyConfigurationForm form = new StatisticsStrategyConfigurationForm(controller);
            form.Show();
        }

        private void QRCodeItem_Click(object sender, EventArgs e)
        {
            QRCodeForm qrCodeForm = new QRCodeForm(controller.GetQRCodeForCurrentServer());
            //qrCodeForm.Icon = this.Icon;
            // TODO
            qrCodeForm.Show();
        }

        private void ScanQRCodeItem_Click(object sender, EventArgs e)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                using (Bitmap fullImage = new Bitmap(screen.Bounds.Width,
                                                screen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(fullImage))
                    {
                        g.CopyFromScreen(screen.Bounds.X,
                                         screen.Bounds.Y,
                                         0, 0,
                                         fullImage.Size,
                                         CopyPixelOperation.SourceCopy);
                    }
                    int maxTry = 10;
                    for (int i = 0; i < maxTry; i++)
                    {
                        int marginLeft = (int)((double)fullImage.Width * i / 2.5 / maxTry);
                        int marginTop = (int)((double)fullImage.Height * i / 2.5 / maxTry);
                        Rectangle cropRect = new Rectangle(marginLeft, marginTop, fullImage.Width - marginLeft * 2, fullImage.Height - marginTop * 2);
                        Bitmap target = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);

                        double imageScale = (double)screen.Bounds.Width / (double)cropRect.Width;
                        using (Graphics g = Graphics.FromImage(target))
                        {
                            g.DrawImage(fullImage, new Rectangle(0, 0, target.Width, target.Height),
                                            cropRect,
                                            GraphicsUnit.Pixel);
                        }
                        var source = new BitmapLuminanceSource(target);
                        var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                        QRCodeReader reader = new QRCodeReader();
                        var result = reader.decode(bitmap);
                        if (result != null)
                        {
                            var success = controller.AddServerBySSURL(result.Text);
                            QRCodeSplashForm splash = new QRCodeSplashForm();
                            if (success)
                            {
                                splash.FormClosed += splash_FormClosed;
                            }
                            else if (result.Text.StartsWith("http://") || result.Text.StartsWith("https://"))
                            {
                                _urlToOpen = result.Text;
                                splash.FormClosed += openURLFromQRCode;
                            }
                            else
                            {
                                MessageBox.Show(I18N.GetString("Failed to decode QRCode"));
                                return;
                            }
                            double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                            foreach (ResultPoint point in result.ResultPoints)
                            {
                                minX = Math.Min(minX, point.X);
                                minY = Math.Min(minY, point.Y);
                                maxX = Math.Max(maxX, point.X);
                                maxY = Math.Max(maxY, point.Y);
                            }
                            minX /= imageScale;
                            minY /= imageScale;
                            maxX /= imageScale;
                            maxY /= imageScale;
                            // make it 20% larger
                            double margin = (maxX - minX) * 0.20f;
                            minX += -margin + marginLeft;
                            maxX += margin + marginLeft;
                            minY += -margin + marginTop;
                            maxY += margin + marginTop;
                            splash.Location = new Point(screen.Bounds.X, screen.Bounds.Y);
                            // we need a panel because a window has a minimal size
                            // TODO: test on high DPI
                            splash.TargetRect = new Rectangle((int)minX + screen.Bounds.X, (int)minY + screen.Bounds.Y, (int)maxX - (int)minX, (int)maxY - (int)minY);
                            splash.Size = new Size(fullImage.Width, fullImage.Height);
                            splash.Show();
                            return;
                        }
                    }
                }
            }
            MessageBox.Show(I18N.GetString("No QRCode found. Try to zoom in or move it to the center of the screen."));
        }

        void splash_FormClosed(object sender, FormClosedEventArgs e)
        {
            ShowConfigForm();
        }

        void openURLFromQRCode(object sender, FormClosedEventArgs e)
        {
            Process.Start(_urlToOpen);
        }

        private void AutoStartupItem_Click(object sender, EventArgs e) {
            AutoStartupItem.Checked = !AutoStartupItem.Checked;
            if (!AutoStartup.Set(AutoStartupItem.Checked)) {
                MessageBox.Show(I18N.GetString("Failed to update registry"));
            }
        }

        private void LocalPACItem_Click(object sender, EventArgs e)
        {
            if (!localPACItem.Checked)
            {
                localPACItem.Checked = true;
                onlinePACItem.Checked = false;
                controller.UseOnlinePAC(false);
                UpdatePACItemsEnabledStatus();
            }
        }

        private void OnlinePACItem_Click(object sender, EventArgs e)
        {
            if (!onlinePACItem.Checked)
            {
                if (String.IsNullOrEmpty(controller.GetConfigurationCopy().pacUrl))
                {
                    UpdateOnlinePACURLItem_Click(sender, e);
                }
                if (!String.IsNullOrEmpty(controller.GetConfigurationCopy().pacUrl))
                {
                    localPACItem.Checked = false;
                    onlinePACItem.Checked = true;
                    controller.UseOnlinePAC(true);
                }
                UpdatePACItemsEnabledStatus();
            }
        }

        private void UpdateOnlinePACURLItem_Click(object sender, EventArgs e)
        {
            string origPacUrl = controller.GetConfigurationCopy().pacUrl;
            string pacUrl = Microsoft.VisualBasic.Interaction.InputBox(
                I18N.GetString("Please input PAC Url"),
                I18N.GetString("Edit Online PAC URL"),
                origPacUrl, -1, -1);
            if (!string.IsNullOrEmpty(pacUrl) && pacUrl != origPacUrl)
            {
                controller.SavePACUrl(pacUrl);
            }
        }

        private void UpdatePACItemsEnabledStatus()
        {
            if (this.localPACItem.Checked)
            {
                this.editLocalPACItem.Enabled = true;
                this.updateFromGFWListItem.Enabled = true;
                this.editGFWUserRuleItem.Enabled = true;
                this.editOnlinePACItem.Enabled = false;
            }
            else
            {
                this.editLocalPACItem.Enabled = false;
                this.updateFromGFWListItem.Enabled = false;
                this.editGFWUserRuleItem.Enabled = false;
                this.editOnlinePACItem.Enabled = true;
            }
        }

        private void UpdateUpdateMenu()
        {
            Configuration configuration = controller.GetConfigurationCopy();
            autoCheckUpdatesToggleItem.Checked = configuration.autoCheckUpdate;
        }

        private void autoCheckUpdatesToggleItem_Click(object sender, EventArgs e)
        {
            Configuration configuration = controller.GetConfigurationCopy();
            controller.ToggleCheckingUpdate(!configuration.autoCheckUpdate);
            UpdateUpdateMenu();
        }

        private void checkUpdatesItem_Click(object sender, EventArgs e)
        {
            updateChecker.CheckUpdate(controller.GetConfigurationCopy());
        }
    }
}
