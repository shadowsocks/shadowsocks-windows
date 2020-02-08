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

        private ContextMenu contextMenu;
        private MenuItem disableItem;
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
        private MenuItem secureLocalPacUrlToggleItem;
        private MenuItem autoCheckUpdatesToggleItem;
        private MenuItem checkPreReleaseToggleItem;
        private MenuItem proxyItem;
        private MenuItem hotKeyItem;
        private MenuItem VerboseLoggingToggleItem;
        private MenuItem ShowPluginOutputToggleItem;
        private MenuItem WriteI18NFileItem;

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

            #region Controller events

            controller.ConfigChanged += (o, e) =>
            {
                LoadCurrentConfiguration();
                UpdateTrayIconAndNotifyText();
            };
            controller.TrafficChanged += ChooseIconByTraffic;

            controller.PACFileReadyToOpen += ShowFileInExplorer;
            controller.UserRuleFileReadyToOpen += ShowFileInExplorer;

            // show message events
            controller.Errored += (o, e) =>
                MessageBox.Show(e.GetException().ToString(), I18N.GetString("Shadowsocks Error: {0}", e.GetException().Message));

            controller.UpdatePACFromGFWListCompleted += (o, e) =>
            {
                string result = e.Success
                    ? I18N.GetString("PAC updated")
                    : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
                ShowBalloonTip(I18N.GetString("Shadowsocks"), result, ToolTipIcon.Info, 1000);
            }
;
            controller.UpdatePACFromGFWListError += (o, e) =>
            {
                ShowBalloonTip(I18N.GetString("Failed to update PAC file"), e.GetException().Message, ToolTipIcon.Error, 5000);
                logger.LogUsefulException(e.GetException());
            };
            // toggle item events
            controller.EnableStatusChanged += (o, e) => disableItem.Checked = !controller.GetConfigurationCopy().enabled;
            controller.ShareOverLANStatusChanged += (o, e) => ShareOverLANItem.Checked = controller.GetConfigurationCopy().shareOverLan;
            controller.VerboseLoggingStatusChanged += (o, e) => VerboseLoggingToggleItem.Checked = controller.GetConfigurationCopy().isVerboseLogging; ;
            controller.ShowPluginOutputChanged += (o, e) => ShowPluginOutputToggleItem.Checked = controller.GetConfigurationCopy().showPluginOutput; ;
            controller.EnableGlobalChanged += (sender, e) =>
            {
                globalModeItem.Checked = controller.GetConfigurationCopy().global;
                PACModeItem.Checked = !globalModeItem.Checked;
            };
            #endregion

            #region Notify icon

            _notifyIcon = new NotifyIcon();
            UpdateTrayIconAndNotifyText();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu;
            _notifyIcon.BalloonTipClicked += (o, e) =>
            {
                if (updateChecker.NewVersionFound)
                {
                    updateChecker.NewVersionFound = false; /* Reset the flag */
                    if (System.IO.File.Exists(updateChecker.LatestVersionLocalName))
                    {
                        string argument = "/select, \"" + updateChecker.LatestVersionLocalName + "\"";
                        System.Diagnostics.Process.Start("explorer.exe", argument);
                    }
                }
            };
            _notifyIcon.MouseClick += (o, e) =>
            {
                UpdateTrayIconAndNotifyText();
                if (e.Button == MouseButtons.Middle)
                {
                    ShowLogForm();
                }
            };
            _notifyIcon.MouseDoubleClick += (o, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowConfigForm();
                }
            };
            _notifyIcon.BalloonTipClosed += (o, e) =>
            {
                if (updateChecker.NewVersionFound)
                {
                    updateChecker.NewVersionFound = false; /* Reset the flag */
                }
            };

            #endregion

            updateChecker = new UpdateChecker();
            updateChecker.CheckUpdateCompleted += (o, e) =>
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
            };

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

        private void ChooseIconByTraffic(object sender, EventArgs e)
        {
            if (icon == null)
            {
                return;
            }

            Icon newIcon;

            bool hasInbound = controller.trafficPerSecondQueue.Last().inboundIncreasement > 0;
            bool hasOutbound = controller.trafficPerSecondQueue.Last().outboundIncreasement > 0;

            if (hasInbound && hasOutbound)
            {
                newIcon = icon_both;
            }
            else if (hasInbound)
            {
                newIcon = icon_in;
            }
            else if (hasOutbound)
            {
                newIcon = icon_out;
            }
            else
            {
                newIcon = icon;
            }

            if (newIcon != previousIcon)
            {
                previousIcon = newIcon;
                _notifyIcon.Icon = newIcon;
            }
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

            string serverInfo = controller.GetCurrentStrategy()?.Name ?? config.GetCurrentServer().ToString();
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

        #region MenuItems and MenuGroups

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
            contextMenu = new ContextMenu(new MenuItem[] {
                CreateMenuGroup("System Proxy", new MenuItem[] {
                    disableItem = CreateMenuItem("Disable", (o,e)=>
                    {
                        controller.ToggleEnable(false);
                        Configuration config = controller.GetConfigurationCopy();
                        UpdateSystemProxyItemsEnabledStatus(config);
                    }),
                    PACModeItem = CreateMenuItem("PAC", (o,e)=>
                    {
                        controller.ToggleEnable(true);
                        controller.ToggleGlobal(false);
                        Configuration config = controller.GetConfigurationCopy();
                        UpdateSystemProxyItemsEnabledStatus(config);
                    }),
                    globalModeItem = CreateMenuItem("Global", (o,e)=>
                    {
                        controller.ToggleEnable(true);
                        controller.ToggleGlobal(true);
                        Configuration config = controller.GetConfigurationCopy();
                        UpdateSystemProxyItemsEnabledStatus(config);
                    })
                }),
                ServersItem = CreateMenuGroup("Servers", new MenuItem[] {
                    SeperatorItem = new MenuItem("-"),
                    ConfigItem = CreateMenuItem("Edit Servers...", (o,e)=>ShowConfigForm()),
                    CreateMenuItem("Statistics Config...", (o,e)=>new StatisticsStrategyConfigurationForm(controller).Show()),
                    new MenuItem("-"),
                    CreateMenuItem("Share Server Config...", (o,e)=> new QRCodeForm(controller.GetServerURLForCurrentServer()).Show()),
                    CreateMenuItem("Scan QRCode from Screen...", ScanQRCode),
                    CreateMenuItem("Import URL from Clipboard...",(o,e)=>
                    {
                        if (controller.AddServerBySSURL(Clipboard.GetText(TextDataFormat.Text))) { ShowConfigForm(); } })
                }),
                CreateMenuGroup("PAC ", new MenuItem[] {
                    localPACItem = CreateMenuItem("Local PAC", (o,e)=>
                    {
                        if (localPACItem.Checked) { return; } localPACItem.Checked = true;
                        onlinePACItem.Checked = false;
                        controller.UseOnlinePAC(false);
                        UpdatePACItemsEnabledStatus();
                    }),
                    onlinePACItem = CreateMenuItem("Online PAC", (o,e)=>{
                        if (onlinePACItem.Checked) { return; } if (controller.GetConfigurationCopy().pacUrl.IsNullOrEmpty())
                        {
                            AskForOnlinePACURL(o, e);
                        }
                        // when user inputed invalid PAC, pacUrl == null
                        if (!controller.GetConfigurationCopy().pacUrl.IsNullOrEmpty())
                        {
                            localPACItem.Checked = false;
                            onlinePACItem.Checked = true;
                            controller.UseOnlinePAC(true);
                        }
                        UpdatePACItemsEnabledStatus();
                    }),
                    new MenuItem("-"),
                    editLocalPACItem = CreateMenuItem("Edit Local PAC File...",(o,e)=> controller.TouchPACFile()),
                    updateFromGFWListItem = CreateMenuItem("Update Local PAC from GFWList", (o,e)=> controller.UpdatePACFromGFWList()),
                    editGFWUserRuleItem = CreateMenuItem("Edit User Rule for GFWList...", (o,e)=>controller.TouchUserRuleFile()),
                    secureLocalPacUrlToggleItem = CreateMenuItem("Secure Local PAC", (o,e)=>
                    {
                        Configuration configuration = controller.GetConfigurationCopy();
                        controller.ToggleSecureLocalPac(!configuration.secureLocalPac);
                    }),
                    CreateMenuItem("Copy Local PAC URL", (o, e)=>controller.CopyPacUrl()),
                    editOnlinePACItem = CreateMenuItem("Edit Online PAC URL...", AskForOnlinePACURL),
                }),
                proxyItem = CreateMenuItem("Forward Proxy...", (o, e) => ShowProxyForm()),
                new MenuItem("-"),
                AutoStartupItem = CreateMenuItem("Start on Boot",(o,e)=>
                {
                    if (!AutoStartup.Set(AutoStartupItem.Checked))
                    {
                        MessageBox.Show(I18N.GetString("Failed to update registry"));
                        return;
                    }
                    AutoStartupItem.Checked = !AutoStartupItem.Checked;
                }),
                ShareOverLANItem = CreateMenuItem("Allow other Devices to connect", (o,e)=>
                {
                    ShareOverLANItem.Checked = !ShareOverLANItem.Checked;
                    controller.ToggleShareOverLAN(ShareOverLANItem.Checked);
                }),
                new MenuItem("-"),
                hotKeyItem = CreateMenuItem("Edit Hotkeys...", (o, e) => ShowHotKeySettingsForm()),
                CreateMenuGroup("Help", new MenuItem[] {
                    CreateMenuItem("Show Logs...", (o, e) => ShowLogForm()),
                    VerboseLoggingToggleItem = CreateMenuItem("Verbose Logging",(o,e)=>
                    {
                        VerboseLoggingToggleItem.Checked = !VerboseLoggingToggleItem.Checked;
                        controller.ToggleVerboseLogging(VerboseLoggingToggleItem.Checked);
                    }),
                    ShowPluginOutputToggleItem = CreateMenuItem("Show Plugin Output",(o,e)=>
                    {
                        ShowPluginOutputToggleItem.Checked = !ShowPluginOutputToggleItem.Checked;
                        controller.ToggleShowPluginOutput(ShowPluginOutputToggleItem.Checked);
                    }),
                    WriteI18NFileItem = CreateMenuItem("Write translation template", (o,e)=> File.WriteAllText(I18N.I18N_FILE, Resources.i18n_csv, Encoding.UTF8)),
                    CreateMenuGroup("Updates...", new MenuItem[] {
                        CreateMenuItem("Check for Updates...", (o, e)=>updateChecker.CheckUpdate(controller.GetConfigurationCopy())),
                        new MenuItem("-"),
                        autoCheckUpdatesToggleItem = CreateMenuItem("Check for Updates at Startup", (o,e)=>
                        {
                            Configuration configuration = controller.GetConfigurationCopy();
                            controller.ToggleCheckingUpdate(!configuration.autoCheckUpdate);
                            UpdateUpdateMenu();
                        }),
                        checkPreReleaseToggleItem = CreateMenuItem("Check Pre-release Version", (o,e)=>
                        {
                            Configuration configuration = controller.GetConfigurationCopy();
                            controller.ToggleCheckingPreRelease(!configuration.checkPreRelease);
                            UpdateUpdateMenu();
                        }),
                    }),
                    CreateMenuItem("About...", (o,e)=>Process.Start("https://github.com/shadowsocks/shadowsocks-windows")),
                }),
                new MenuItem("-"),
                CreateMenuItem("Quit",(o,e)=>
                {
                    controller.Stop();
                    _notifyIcon.Visible = false;
                    Application.Exit();
                })
            });
        }

        #endregion

        void ShowFileInExplorer(object sender, ShadowsocksController.PathEventArgs e)
        {
            Process.Start("explorer.exe", @"/select, " + e.Path);
        }

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
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
            Menu.MenuItemCollection items = ServersItem.MenuItems;
            while (items[0] != SeperatorItem)
            {
                items.RemoveAt(0);
            }
            int strategyCount = 0;
            foreach (Controller.Strategy.IStrategy strategy in controller.GetStrategies())
            {
                MenuItem item = new MenuItem(strategy.Name)
                {
                    Tag = strategy.ID
                };
                item.Click += (o, e) => controller.SelectStrategy((string)((MenuItem)o).Tag);
                items.Add(strategyCount, item);
                strategyCount++;
            }

            // user wants a seperator item between strategy and servers menugroup
            items.Add(strategyCount++, new MenuItem("-"));

            int serverCount = 0;
            Configuration configuration = controller.GetConfigurationCopy();
            foreach (Server server in configuration.configs)
            {
                if (Configuration.ChecksServer(server))
                {
                    MenuItem item = new MenuItem(server.ToString())
                    {
                        Tag = configuration.configs.FindIndex(s => s == server)
                    };
                    item.Click += (o, e) => controller.SelectServerIndex((int)((MenuItem)o).Tag);
                    items.Add(strategyCount + serverCount, item);
                    serverCount++;
                }
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
                configForm.Activate();
                configForm.FormClosed += (object sender, FormClosedEventArgs e) =>
                {
                    DisposeForm(configForm)(sender, e);
                    if (_isFirstRun)
                    {
                        CheckUpdateForFirstRun();
                        ShowFirstTimeBalloon();
                        _isFirstRun = false;
                    }
                };
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
                proxyForm.FormClosed += DisposeForm(proxyForm);
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
                hotkeySettingsForm.FormClosed += DisposeForm(hotkeySettingsForm);
            }
        }

        public void ShowLogForm()
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
                logForm.FormClosed += DisposeForm(logForm);
            }
        }

        FormClosedEventHandler DisposeForm(Form f)
        {
            return (object o, FormClosedEventArgs e) =>
            {
                f?.Dispose();
                f = null;
                Utils.ReleaseMemory(true);
            };
        }

        private void CheckUpdateForFirstRun()
        {
            Configuration config = controller.GetConfigurationCopy();
            if (config.isDefault)
            {
                return;
            }

            _isStartupChecking = true;
            updateChecker.CheckUpdate(config, 3000);
        }

        private void ShowFirstTimeBalloon()
        {
            _notifyIcon.BalloonTipTitle = I18N.GetString("Shadowsocks is here");
            _notifyIcon.BalloonTipText = I18N.GetString("You can turn on/off Shadowsocks in the context menu");
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(0);
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

        private void ScanQRCode(object sender, EventArgs e)
        {
            string qrcode = QRCodeUtil.ScanScreenQRCode();
            if (qrcode.IsNullOrWhiteSpace())
            {
                MessageBox.Show(I18N.GetString("No QRCode found. Try to zoom in or move it to the center of the screen."));
                return;
            }
            bool success = controller.AddServerBySSURL(qrcode);
            if (success)
            {
                ShowConfigForm();
            }
            else if (qrcode.ToLower().StartsWith("http://") || qrcode.ToLower().StartsWith("https://"))
            {
                _urlToOpen = qrcode;
                Process.Start(_urlToOpen);
            }
            else
            {
                MessageBox.Show(I18N.GetString("Failed to decode QRCode"));
                return;
            }
        }

        private void AskForOnlinePACURL(object o, EventArgs e)
        {
            string origPacUrl = controller.GetConfigurationCopy().pacUrl;
            string pacUrl = Microsoft.VisualBasic.Interaction.InputBox(
                I18N.GetString("Please input PAC Url"),
                I18N.GetString("Edit Online PAC URL"),
                origPacUrl, -1, -1);
            if (!pacUrl.IsNullOrEmpty() && pacUrl != origPacUrl)
            {
                controller.SavePACUrl(pacUrl);
            }
        }

        private void UpdatePACItemsEnabledStatus()
        {
            if (localPACItem.Checked)
            {
                editLocalPACItem.Enabled = true;
                updateFromGFWListItem.Enabled = true;
                editGFWUserRuleItem.Enabled = true;
                editOnlinePACItem.Enabled = false;
            }
            else
            {
                editLocalPACItem.Enabled = false;
                updateFromGFWListItem.Enabled = false;
                editGFWUserRuleItem.Enabled = false;
                editOnlinePACItem.Enabled = true;
            }
        }

        private void UpdateUpdateMenu()
        {
            Configuration configuration = controller.GetConfigurationCopy();
            autoCheckUpdatesToggleItem.Checked = configuration.autoCheckUpdate;
            checkPreReleaseToggleItem.Checked = configuration.checkPreRelease;
        }
    }
}
