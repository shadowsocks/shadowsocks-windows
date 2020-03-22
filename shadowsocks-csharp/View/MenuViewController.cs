using NLog;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Shadowsocks.View
{
    public class MenuViewController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        // yes this is just a menu view controller
        // when config form is closed, it moves away from RAM
        // and it should just do anything related to the config form

        private ShadowsocksController controller;
        private UpdateChecker updateChecker;

        private NotifyIcon _notifyIcon;
        private Icon icon, icon_in, icon_out, icon_both, previousIcon;

        private bool _isFirstRun;
        private bool _isStartupChecking;
        private string _urlToOpen;

        private ContextMenuStrip contextMenu1;
        private ToolStripMenuItem disableItem;
        private ToolStripMenuItem AutoStartupItem;
        private ToolStripMenuItem ShareOverLANItem;
        private ToolStripSeparator SeperatorItem;
        private ToolStripMenuItem ConfigItem;
        private ToolStripMenuItem ServersItem;
        private ToolStripMenuItem globalModeItem;
        private ToolStripMenuItem PACModeItem;
        private ToolStripMenuItem localPACItem;
        private ToolStripMenuItem onlinePACItem;
        private ToolStripMenuItem editLocalPACItem;
        private ToolStripMenuItem updateFromGFWListItem;
        private ToolStripMenuItem editGFWUserRuleItem;
        private ToolStripMenuItem editOnlinePACItem;
        private ToolStripMenuItem secureLocalPacUrlToggleItem;
        private ToolStripMenuItem autoCheckUpdatesToggleItem;
        private ToolStripMenuItem checkPreReleaseToggleItem;
        private ToolStripMenuItem proxyItem;
        private ToolStripMenuItem hotKeyItem;
        private ToolStripMenuItem VerboseLoggingToggleItem;
        private ToolStripMenuItem ShowPluginOutputToggleItem;
        private ToolStripMenuItem WriteI18NFileItem;

        private ConfigForm configForm;
        private ProxyForm proxyForm;
        private LogForm logForm;
        private HotkeySettingsForm hotkeySettingsForm;



        // color definition for icon color transformation
        private readonly Color colorMaskBlue = Color.FromArgb(255, 25, 125, 191);
        private readonly Color colorMaskDarkSilver = Color.FromArgb(128, 192, 192, 192);
        private readonly Color colorMaskLightSilver = Color.FromArgb(192, 192, 192);
        private readonly Color colorMaskEclipse = Color.FromArgb(192, 64, 64, 64);

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();

            controller.EnableStatusChanged += controller_EnableStatusChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_FileReadyToOpen;
            controller.UserRuleFileReadyToOpen += controller_FileReadyToOpen;
            controller.ShareOverLANStatusChanged += controller_ShareOverLANStatusChanged;
            controller.VerboseLoggingStatusChanged += controller_VerboseLoggingStatusChanged;
            controller.ShowPluginOutputChanged += controller_ShowPluginOutputChanged;
            controller.EnableGlobalChanged += controller_EnableGlobalChanged;
            controller.Errored += controller_Errored;
            controller.UpdatePACFromGFWListCompleted += controller_UpdatePACFromGFWListCompleted;
            controller.UpdatePACFromGFWListError += controller_UpdatePACFromGFWListError;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIconAndNotifyText();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = contextMenu1;
            _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;
            _notifyIcon.MouseClick += notifyIcon1_Click;
            _notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;
            _notifyIcon.BalloonTipClosed += _notifyIcon_BalloonTipClosed;
            controller.TrafficChanged += controller_TrafficChanged;

            this.updateChecker = new UpdateChecker();
            updateChecker.CheckUpdateCompleted += updateChecker_CheckUpdateCompleted;

            LoadCurrentConfiguration();

            Configuration config = controller.GetConfigurationCopy();

            if (config.isDefault)
            {
                _isFirstRun = true;
                ShowConfigForm();
            }
            else if (config.autoCheckUpdate)
            {
                _isStartupChecking = true;
                updateChecker.CheckUpdate(config, 3000);
            }
        }

        private void controller_TrafficChanged(object sender, EventArgs e)
        {
            if (icon == null)
                return;

            Icon newIcon;

            bool hasInbound = controller.trafficPerSecondQueue.Last().inboundIncreasement > 0;
            bool hasOutbound = controller.trafficPerSecondQueue.Last().outboundIncreasement > 0;

            if (hasInbound && hasOutbound)
                newIcon = icon_both;
            else if (hasInbound)
                newIcon = icon_in;
            else if (hasOutbound)
                newIcon = icon_out;
            else
                newIcon = icon;

            if (newIcon != this.previousIcon)
            {
                this.previousIcon = newIcon;
                _notifyIcon.Icon = newIcon;
            }
        }

        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(), I18N.GetString("Shadowsocks Error: {0}", e.GetException().Message));
        }

        #region Tray Icon

        private void UpdateTrayIconAndNotifyText()
        {
            Configuration config = controller.GetConfigurationCopy();
            bool enabled = config.enabled;
            bool global = config.global;

            Color colorMask = SelectColorMask(enabled, global);
            Size iconSize = SelectIconSize();

            UpdateIconSet(colorMask, iconSize, out icon, out icon_in, out icon_out, out icon_both);

            previousIcon = icon;
            _notifyIcon.Icon = previousIcon;

            string serverInfo = null;
            if (controller.GetCurrentStrategy() != null)
            {
                serverInfo = controller.GetCurrentStrategy().Name;
            }
            else
            {
                serverInfo = config.GetCurrentServer().FriendlyName();
            }
            // show more info by hacking the P/Invoke declaration for NOTIFYICONDATA inside Windows Forms
            string text = I18N.GetString("Shadowsocks") + " " + UpdateChecker.Version + "\n" +
                          (enabled ?
                              I18N.GetString("System Proxy On: ") + (global ? I18N.GetString("Global") : I18N.GetString("PAC")) :
                              I18N.GetString("Running: Port {0}", config.localPort))  // this feedback is very important because they need to know Shadowsocks is running
                          + "\n" + serverInfo;
            if (text.Length > 127)
            {
                text = text.Substring(0, 126 - 3) + "...";
            }
            ViewUtils.SetNotifyIconText(_notifyIcon, text);
        }

        /// <summary>
        /// Determine the icon size based on the screen DPI.
        /// </summary>
        /// <returns></returns>
        /// https://stackoverflow.com/a/40851713/2075611
        private Size SelectIconSize()
        {
            Size size = new Size(32, 32);
            int dpi = ViewUtils.GetScreenDpi();
            if (dpi < 97)
            {
                // dpi = 96;
                size = new Size(16, 16);
            }
            else if (dpi < 121)
            {
                // dpi = 120;
                size = new Size(20, 20);
            }
            else if (dpi < 145)
            {
                // dpi = 144;
                size = new Size(24, 24);
            }
            else
            {
                // dpi = 168;
                size = new Size(28, 28);
            }
            return size;
        }

        private Color SelectColorMask(bool isProxyEnabled, bool isGlobalProxy)
        {
            Color colorMask = Color.White;

            Utils.WindowsThemeMode currentWindowsThemeMode = Utils.GetWindows10SystemThemeSetting();

            if (isProxyEnabled)
            {
                if (isGlobalProxy)  // global
                {
                    colorMask = colorMaskBlue;
                }
                else  // PAC
                {
                    if (currentWindowsThemeMode == Utils.WindowsThemeMode.Light)
                    {
                        colorMask = colorMaskEclipse;
                    }
                }
            }
            else  // disabled
            {
                if (currentWindowsThemeMode == Utils.WindowsThemeMode.Light)
                {
                    colorMask = colorMaskDarkSilver;
                }
                else
                {
                    colorMask = colorMaskLightSilver;
                }
            }

            return colorMask;
        }

        private void UpdateIconSet(Color colorMask, Size size,
            out Icon icon, out Icon icon_in, out Icon icon_out, out Icon icon_both)
        {
            Bitmap iconBitmap;

            // generate the base icon
            iconBitmap = ViewUtils.ChangeBitmapColor(Resources.ss32Fill, colorMask);
            iconBitmap = ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32Outline);

            icon = Icon.FromHandle(ViewUtils.ResizeBitmap(iconBitmap, size.Width, size.Height).GetHicon());
            icon_in = Icon.FromHandle(ViewUtils.ResizeBitmap(ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32In), size.Width, size.Height).GetHicon());
            icon_out = Icon.FromHandle(ViewUtils.ResizeBitmap(ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32In), size.Width, size.Height).GetHicon());
            icon_both = Icon.FromHandle(ViewUtils.ResizeBitmap(ViewUtils.AddBitmapOverlay(iconBitmap, Resources.ss32In, Resources.ss32Out), size.Width, size.Height).GetHicon());
        }



        #endregion

        #region ToolStripMenuItems and MenuGroups

        private ToolStripMenuItem CreateToolStripMenuItem(string text, EventHandler click)
        {
            return new ToolStripMenuItem(I18N.GetString(text), null, click);
        }

        private ToolStripMenuItem CreateMenuGroup(string text, ToolStripItem[] items)
        {
            return new ToolStripMenuItem(I18N.GetString(text), null, items);
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new ContextMenuStrip();
            contextMenu1.Items.AddRange(new ToolStripItem[]{
                CreateMenuGroup("System Proxy", new ToolStripMenuItem[] {
                    this.disableItem = CreateToolStripMenuItem("Disable", new EventHandler(this.EnableItem_Click)),
                    this.PACModeItem = CreateToolStripMenuItem("PAC", new EventHandler(this.PACModeItem_Click)),
                    this.globalModeItem = CreateToolStripMenuItem("Global", new EventHandler(this.GlobalModeItem_Click))
                }),
                this.ServersItem = CreateMenuGroup("Servers", new  ToolStripItem [] {
                    this.SeperatorItem = new ToolStripSeparator(),
                    this.ConfigItem = CreateToolStripMenuItem("Edit Servers...", new EventHandler(this.Config_Click)),
                    CreateToolStripMenuItem("Statistics Config...", StatisticsConfigItem_Click),
                    new ToolStripSeparator(),
                    CreateToolStripMenuItem("Share Server Config...", new EventHandler(this.QRCodeItem_Click)),
                    CreateToolStripMenuItem("Scan QRCode from Screen...", new EventHandler(this.ScanQRCodeItem_Click)),
                    CreateToolStripMenuItem("Import URL from Clipboard...", new EventHandler(this.ImportURLItem_Click))
                }),
                CreateMenuGroup("PAC ", new ToolStripItem[] {
                    this.localPACItem = CreateToolStripMenuItem("Local PAC", new EventHandler(this.LocalPACItem_Click)),
                    this.onlinePACItem = CreateToolStripMenuItem("Online PAC", new EventHandler(this.OnlinePACItem_Click)),
                    new ToolStripSeparator(),
                    this.editLocalPACItem = CreateToolStripMenuItem("Edit Local PAC File...", new EventHandler(this.EditPACFileItem_Click)),
                    this.updateFromGFWListItem = CreateToolStripMenuItem("Update Local PAC from GFWList", new EventHandler(this.UpdatePACFromGFWListItem_Click)),
                    this.editGFWUserRuleItem = CreateToolStripMenuItem("Edit User Rule for GFWList...", new EventHandler(this.EditUserRuleFileForGFWListItem_Click)),
                    this.secureLocalPacUrlToggleItem = CreateToolStripMenuItem("Secure Local PAC", new EventHandler(this.SecureLocalPacUrlToggleItem_Click)),
                    CreateToolStripMenuItem("Copy Local PAC URL", new EventHandler(this.CopyLocalPacUrlItem_Click)),
                    this.editOnlinePACItem = CreateToolStripMenuItem("Edit Online PAC URL...", new EventHandler(this.UpdateOnlinePACURLItem_Click)),
                }),
                this.proxyItem = CreateToolStripMenuItem("Forward Proxy...", new EventHandler(this.proxyItem_Click)),
                new ToolStripSeparator(),
                this.AutoStartupItem = CreateToolStripMenuItem("Start on Boot", new EventHandler(this.AutoStartupItem_Click)),
                this.ShareOverLANItem = CreateToolStripMenuItem("Allow other Devices to connect", new EventHandler(this.ShareOverLANItem_Click)),
                new ToolStripSeparator(),
                this.hotKeyItem = CreateToolStripMenuItem("Edit Hotkeys...", new EventHandler(this.hotKeyItem_Click)),
                CreateMenuGroup("Help", new ToolStripItem[] {
                    CreateToolStripMenuItem("Show Logs...", new EventHandler(this.ShowLogItem_Click)),
                    this.VerboseLoggingToggleItem = CreateToolStripMenuItem( "Verbose Logging", new EventHandler(this.VerboseLoggingToggleItem_Click) ),
                    this.ShowPluginOutputToggleItem = CreateToolStripMenuItem("Show Plugin Output", new EventHandler(this.ShowPluginOutputToggleItem_Click)),
                    this.WriteI18NFileItem = CreateToolStripMenuItem("Write translation template",new EventHandler(WriteI18NFileItem_Click)),
                    CreateMenuGroup("Updates...", new ToolStripItem[] {
                        CreateToolStripMenuItem("Check for Updates...", new EventHandler(this.checkUpdatesItem_Click)),
                        new ToolStripSeparator(),
                        this.autoCheckUpdatesToggleItem = CreateToolStripMenuItem("Check for Updates at Startup", new EventHandler(this.autoCheckUpdatesToggleItem_Click)),
                        this.checkPreReleaseToggleItem = CreateToolStripMenuItem("Check Pre-release Version", new EventHandler(this.checkPreReleaseToggleItem_Click)),
                    }),
                    CreateToolStripMenuItem("About...", new EventHandler(this.AboutItem_Click)),
                }),
                new ToolStripSeparator(),
                CreateToolStripMenuItem("Quit", new EventHandler(this.Quit_Click))
            });
        }

        #endregion

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIconAndNotifyText();
        }

        private void controller_EnableStatusChanged(object sender, EventArgs e)
        {
            disableItem.Checked = !controller.GetConfigurationCopy().enabled;
        }

        void controller_ShareOverLANStatusChanged(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = controller.GetConfigurationCopy().shareOverLan;
        }

        void controller_VerboseLoggingStatusChanged(object sender, EventArgs e)
        {
            VerboseLoggingToggleItem.Checked = controller.GetConfigurationCopy().isVerboseLogging;
        }

        void controller_ShowPluginOutputChanged(object sender, EventArgs e)
        {
            ShowPluginOutputToggleItem.Checked = controller.GetConfigurationCopy().showPluginOutput;
        }

        void controller_EnableGlobalChanged(object sender, EventArgs e)
        {
            globalModeItem.Checked = controller.GetConfigurationCopy().global;
            PACModeItem.Checked = !globalModeItem.Checked;
        }

        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            Process.Start("explorer.exe", argument);
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
            logger.LogUsefulException(e.GetException());
        }

        void controller_UpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            string result = e.Success
                ? I18N.GetString("PAC updated")
                : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
            ShowBalloonTip(I18N.GetString("Shadowsocks"), result, ToolTipIcon.Info, 1000);
        }

        void updateChecker_CheckUpdateCompleted(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks {0} Update Found", updateChecker.LatestVersionNumber + updateChecker.LatestVersionSuffix), I18N.GetString("Click here to update"), ToolTipIcon.Info, 5000);
            }
            else if (!_isStartupChecking)
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("No update is available"), ToolTipIcon.Info, 5000);
            }
            _isStartupChecking = false;
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                updateChecker.NewVersionFound = false; /* Reset the flag */
                if (File.Exists(updateChecker.LatestVersionLocalName))
                {
                    string argument = "/select, \"" + updateChecker.LatestVersionLocalName + "\"";
                    Process.Start("explorer.exe", argument);
                }
            }
        }

        private void _notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                updateChecker.NewVersionFound = false; /* Reset the flag */
            }
        }

        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetConfigurationCopy();
            UpdateServersMenu();
            UpdateSystemProxyItemsEnabledStatus(config);
            ShareOverLANItem.Checked = config.shareOverLan;
            VerboseLoggingToggleItem.Checked = config.isVerboseLogging;
            ShowPluginOutputToggleItem.Checked = config.showPluginOutput;
            AutoStartupItem.Checked = AutoStartup.Check();
            onlinePACItem.Checked = onlinePACItem.Enabled && config.useOnlinePac;
            localPACItem.Checked = !onlinePACItem.Checked;
            secureLocalPacUrlToggleItem.Checked = config.secureLocalPac;
            UpdatePACItemsEnabledStatus();
            UpdateUpdateMenu();
        }

        private void UpdateServersMenu()
        {
            var items = ServersItem.DropDownItems;
            while (items[0] != SeperatorItem)
            {
                items.RemoveAt(0);
            }
            int strategyCount = 0;
            foreach (var strategy in controller.GetStrategies())
            {
                if (!items.OfType<ToolStripItem>().Any(ts => ts.Text == strategy.Name))
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(strategy.Name);
                    item.Tag = strategy.ID;
                    item.Click += AStrategyItem_Click;
                    items.Add(item);
                    strategyCount++;
                }
            }
            if (!items.OfType<ToolStripSeparator>().Any(ts => ts.Tag?.ToString() == "-server-"))
            {
                // user wants a seperator item between strategy and servers menugroup
                items.Add(new ToolStripSeparator() { Tag = "-server-" });
            }
            int serverCount = 0;
            Configuration configuration = controller.GetConfigurationCopy();
            foreach (var server in configuration.configs)
            {
                if (Configuration.ChecksServer(server))
                {
                    var name = server.FriendlyName();
                    if (!items.OfType<ToolStripMenuItem>().Any(ts => ts.Text == name))
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(name);
                        item.Tag = configuration.configs.FindIndex(s => s == server);
                        item.Click += AServerItem_Click;
                        items.Add(item);
                        serverCount++;
                    }
                }
            }

            foreach (var item in items)
            {
                var menuItem = item as ToolStripMenuItem;
                if (menuItem == null || menuItem.Tag == null) continue;

                if (
                    menuItem.Tag.ToString() == configuration.index.ToString()
                    || menuItem.Tag.ToString() == configuration.strategy
                    )
                {
                    menuItem.Checked = true;
                } else
                {
                    menuItem.Checked = false;
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
                configForm.Activate();
                configForm.FormClosed += configForm_FormClosed;
            }
        }

        private void ShowProxyForm()
        {
            if (proxyForm != null)
            {
                proxyForm.Activate();
            }
            else
            {
                proxyForm = new ProxyForm(controller);
                proxyForm.Show();
                proxyForm.Activate();
                proxyForm.FormClosed += proxyForm_FormClosed;
            }
        }

        private void ShowHotKeySettingsForm()
        {
            if (hotkeySettingsForm != null)
            {
                hotkeySettingsForm.Activate();
            }
            else
            {
                hotkeySettingsForm = new HotkeySettingsForm(controller);
                hotkeySettingsForm.Show();
                hotkeySettingsForm.Activate();
                hotkeySettingsForm.FormClosed += hotkeySettingsForm_FormClosed;
            }
        }

        private void ShowLogForm()
        {
            if (logForm != null)
            {
                logForm.Activate();
            }
            else
            {
                logForm = new LogForm(controller);
                logForm.Show();
                logForm.Activate();
                logForm.FormClosed += logForm_FormClosed;
            }
        }

        void logForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            logForm.Dispose();
            logForm = null;
            Utils.ReleaseMemory(true);
        }

        void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            configForm.Dispose();
            configForm = null;
            Utils.ReleaseMemory(true);
            if (_isFirstRun)
            {
                CheckUpdateForFirstRun();
                ShowBalloonTip(
                    I18N.GetString("Shadowsocks is here"),
                    I18N.GetString("You can turn on/off Shadowsocks in the context menu"),
                    ToolTipIcon.Info,
                    0
                );
                _isFirstRun = false;
            }
        }

        void proxyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            proxyForm.Dispose();
            proxyForm = null;
            Utils.ReleaseMemory(true);
        }

        void hotkeySettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            hotkeySettingsForm.Dispose();
            hotkeySettingsForm = null;
            Utils.ReleaseMemory(true);
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

        private void CheckUpdateForFirstRun()
        {
            Configuration config = controller.GetConfigurationCopy();
            if (config.isDefault) return;
            _isStartupChecking = true;
            updateChecker.CheckUpdate(config, 3000);
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/shadowsocks/shadowsocks-windows");
        }

        private void notifyIcon1_Click(object sender, MouseEventArgs e)
        {
            UpdateTrayIconAndNotifyText();
            if (e.Button == MouseButtons.Middle)
            {
                ShowLogForm();
            }
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
            controller.ToggleEnable(false);
            Configuration config = controller.GetConfigurationCopy();
            UpdateSystemProxyItemsEnabledStatus(config);
        }

        private void UpdateSystemProxyItemsEnabledStatus(Configuration config)
        {
            disableItem.Checked = !config.enabled;
            if (!config.enabled)
            {
                globalModeItem.Checked = false;
                PACModeItem.Checked = false;
            }
            else
            {
                globalModeItem.Checked = config.global;
                PACModeItem.Checked = !config.global;
            }
        }

        private void GlobalModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleEnable(true);
            controller.ToggleGlobal(true);
            Configuration config = controller.GetConfigurationCopy();
            UpdateSystemProxyItemsEnabledStatus(config);
        }

        private void PACModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleEnable(true);
            controller.ToggleGlobal(false);
            Configuration config = controller.GetConfigurationCopy();
            UpdateSystemProxyItemsEnabledStatus(config);
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
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            controller.SelectServerIndex((int)item.Tag);
        }

        private void AStrategyItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            controller.SelectStrategy((string)item.Tag);
        }

        private void VerboseLoggingToggleItem_Click(object sender, EventArgs e)
        {
            VerboseLoggingToggleItem.Checked = !VerboseLoggingToggleItem.Checked;
            controller.ToggleVerboseLogging(VerboseLoggingToggleItem.Checked);
        }

        private void ShowPluginOutputToggleItem_Click(object sender, EventArgs e)
        {
            ShowPluginOutputToggleItem.Checked = !ShowPluginOutputToggleItem.Checked;
            controller.ToggleShowPluginOutput(ShowPluginOutputToggleItem.Checked);
        }

        private void WriteI18NFileItem_Click(object sender, EventArgs e)
        {
            File.WriteAllText(I18N.I18N_FILE, Resources.i18n_csv, Encoding.UTF8);
        }

        private void StatisticsConfigItem_Click(object sender, EventArgs e)
        {
            StatisticsStrategyConfigurationForm form = new StatisticsStrategyConfigurationForm(controller);
            form.Show();
        }

        private void QRCodeItem_Click(object sender, EventArgs e)
        {
            QRCodeForm qrCodeForm = new QRCodeForm(controller.GetServerURLForCurrentServer());
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
                        var source = new BitmapSourceLuminanceSource(null);
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
                            else if (result.Text.ToLower().StartsWith("http://") || result.Text.ToLower().StartsWith("https://"))
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
                            splash.TargetRect = new Rectangle((int)minX, (int)minY, (int)maxX - (int)minX, (int)maxY - (int)minY);
                            splash.Size = new Size(fullImage.Width, fullImage.Height);
                            splash.Show();
                            return;
                        }
                    }
                }
            }
            MessageBox.Show(I18N.GetString("No QRCode found. Try to zoom in or move it to the center of the screen."));
        }

        private void ImportURLItem_Click(object sender, EventArgs e)
        {
            var success = controller.AddServerBySSURL(Clipboard.GetText(TextDataFormat.Text));
            if (success)
            {
                ShowConfigForm();
            }
        }

        void splash_FormClosed(object sender, FormClosedEventArgs e)
        {
            ShowConfigForm();
        }

        void openURLFromQRCode(object sender, FormClosedEventArgs e)
        {
            Process.Start(_urlToOpen);
        }

        private void AutoStartupItem_Click(object sender, EventArgs e)
        {
            AutoStartupItem.Checked = !AutoStartupItem.Checked;
            if (!AutoStartup.Set(AutoStartupItem.Checked))
            {
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
                if (controller.GetConfigurationCopy().pacUrl.IsNullOrEmpty())
                {
                    UpdateOnlinePACURLItem_Click(sender, e);
                }
                if (!controller.GetConfigurationCopy().pacUrl.IsNullOrEmpty())
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
#if NET472
            string pacUrl = Microsoft.VisualBasic.Interaction.InputBox(
                I18N.GetString("Please input PAC Url"),
                I18N.GetString("Edit Online PAC URL"),
                origPacUrl, -1, -1);
#else
            string pacUrl = ViewUtils.InputBox(
                I18N.GetString("Please input PAC Url"),
                I18N.GetString("Edit Online PAC URL"),
                origPacUrl, -1, -1);
#endif
            if (!pacUrl.IsNullOrEmpty() && pacUrl != origPacUrl)
            {
                controller.SavePACUrl(pacUrl);
            }
        }

        private void SecureLocalPacUrlToggleItem_Click(object sender, EventArgs e)
        {
            Configuration configuration = controller.GetConfigurationCopy();
            controller.ToggleSecureLocalPac(!configuration.secureLocalPac);
        }

        private void CopyLocalPacUrlItem_Click(object sender, EventArgs e)
        {
            controller.CopyPacUrl();
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
            checkPreReleaseToggleItem.Checked = configuration.checkPreRelease;
        }

        private void autoCheckUpdatesToggleItem_Click(object sender, EventArgs e)
        {
            Configuration configuration = controller.GetConfigurationCopy();
            controller.ToggleCheckingUpdate(!configuration.autoCheckUpdate);
            UpdateUpdateMenu();
        }

        private void checkPreReleaseToggleItem_Click(object sender, EventArgs e)
        {
            Configuration configuration = controller.GetConfigurationCopy();
            controller.ToggleCheckingPreRelease(!configuration.checkPreRelease);
            UpdateUpdateMenu();
        }

        private void checkUpdatesItem_Click(object sender, EventArgs e)
        {
            updateChecker.CheckUpdate(controller.GetConfigurationCopy());
        }

        private void proxyItem_Click(object sender, EventArgs e)
        {
            ShowProxyForm();
        }

        private void hotKeyItem_Click(object sender, EventArgs e)
        {
            ShowHotKeySettingsForm();
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            ShowLogForm();
        }

        public void ShowLogForm_HotKey()
        {
            ShowLogForm();
        }
    }
}
