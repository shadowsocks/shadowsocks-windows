using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using System.Threading;

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

        private MenuItem enableItem;
        private MenuItem modeItem;
        private MenuItem SeperatorItem;
        //private MenuItem ConfigItem;
        private MenuItem ServersItem;
        private MenuItem globalModeItem;
        private MenuItem PACModeItem;
        //private MenuItem editLocalPACItem;
        //private MenuItem updateFromGFWListItem;
        //private MenuItem editGFWUserRuleItem;
        private MenuItem SelectRandomItem;
        private MenuItem sameHostForSameTargetItem;
        private MenuItem httpWhiteListItem;
        private MenuItem UpdateItem;
        private ConfigForm configForm;
        private SettingsForm settingsForm;
        private ServerLogForm serverLogForm;
        private string _urlToOpen;
        private System.Timers.Timer timerDelayCheckUpdate;

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();

            controller.ToggleModeChanged += controller_ToggleModeChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_FileReadyToOpen;
            controller.UserRuleFileReadyToOpen += controller_FileReadyToOpen;
            //controller.ShareOverLANStatusChanged += controller_ShareOverLANStatusChanged;
            controller.Errored += controller_Errored;
            controller.UpdatePACFromGFWListCompleted += controller_UpdatePACFromGFWListCompleted;
            controller.UpdatePACFromGFWListError += controller_UpdatePACFromGFWListError;
            controller.ShowConfigFormEvent += Config_Click;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;
            _notifyIcon.MouseClick += notifyIcon1_Click;
            //_notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;

            this.updateChecker = new UpdateChecker();
            updateChecker.NewVersionFound += updateChecker_NewVersionFound;

            LoadCurrentConfiguration();

            Configuration cfg = controller.GetConfiguration();
            if (cfg.configs.Count == 1 && cfg.configs[0].server == Configuration.GetDefaultServer().server)
            {
                ShowConfigForm(false);
            }

            timerDelayCheckUpdate = new System.Timers.Timer(1000 * 10.0);
            timerDelayCheckUpdate.Elapsed += timer_Elapsed;
            timerDelayCheckUpdate.Start();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            updateChecker.CheckUpdate(controller.GetConfiguration());
            if (timerDelayCheckUpdate != null)
            {
                timerDelayCheckUpdate.Elapsed -= timer_Elapsed;
                timerDelayCheckUpdate.Stop();
                timerDelayCheckUpdate = new System.Timers.Timer(1000.0 * 60 * 60 * 2);
                timerDelayCheckUpdate.Elapsed += timer_Elapsed;
                timerDelayCheckUpdate.Start();
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
            Configuration config = controller.GetConfiguration();
            bool enabled = config.sysProxyMode != (int)ProxyMode.NoModify;
            bool global = config.sysProxyMode == (int)ProxyMode.Global;
            bool random = config.random;
            double mul_a = 1.0, mul_r = 1.0, mul_g = 1.0, mul_b = 1.0;
            if (!enabled)
            {
                mul_g = 0.4;
            }
            else if (!global)
            {
                mul_b = 0.4;
            }
            if (!random)
            {
                mul_r = 0.4;
            }

            {
                Bitmap iconCopy = new Bitmap(icon);
                for (int x = 0; x < iconCopy.Width; x++)
                {
                    for (int y = 0; y < iconCopy.Height; y++)
                    {
                        Color color = icon.GetPixel(x, y);
                        iconCopy.SetPixel(x, y,
                            Color.FromArgb((byte)(color.A * mul_a),
                            ((byte)(color.R * mul_r)),
                            ((byte)(color.G * mul_g)),
                            ((byte)(color.B * mul_b))));
                    }
                }
                icon = iconCopy;
            }
            _notifyIcon.Icon = Icon.FromHandle(icon.GetHicon());

            // we want to show more details but notify icon title is limited to 63 characters
            string text = UpdateChecker.Name + " " + UpdateChecker.FullVersion + "\n" +
                (enabled ?
                    I18N.GetString("System Proxy On: ") + (global ? I18N.GetString("Global") : I18N.GetString("PAC")) :
                    String.Format(I18N.GetString("Running: Port {0}"), config.localPort))  // this feedback is very important because they need to know Shadowsocks is running
                + "\n" + config.GetCurrentServer().FriendlyName();
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
                modeItem = CreateMenuGroup("Mode", new MenuItem[] {
                    enableItem = CreateMenuItem("Disable System Proxy", new EventHandler(this.EnableItem_Click)),
                    PACModeItem = CreateMenuItem("PAC", new EventHandler(this.PACModeItem_Click)),
                    globalModeItem = CreateMenuItem("Global", new EventHandler(this.GlobalModeItem_Click))
                }),
                CreateMenuGroup("PAC ", new MenuItem[] {
                    CreateMenuItem("Update Local PAC from Lan IP List", new EventHandler(this.UpdatePACFromLanIPListItem_Click)),
                    CreateMenuItem("Update Local PAC from Chn White List", new EventHandler(this.UpdatePACFromCNWhiteListItem_Click)),
                    CreateMenuItem("Update Local PAC from Chn IP List", new EventHandler(this.UpdatePACFromCNIPListItem_Click)),
                    CreateMenuItem("Update Local PAC from GFWList", new EventHandler(this.UpdatePACFromGFWListItem_Click)),
                    new MenuItem("-"),
                    CreateMenuItem("Update Local PAC from Chn Only List", new EventHandler(this.UpdatePACFromCNOnlyListItem_Click)),
                    new MenuItem("-"),
                    CreateMenuItem("Edit Local PAC File...", new EventHandler(this.EditPACFileItem_Click)),
                    CreateMenuItem("Edit User Rule for GFWList...", new EventHandler(this.EditUserRuleFileForGFWListItem_Click)),
                }),
                new MenuItem("-"),
                ServersItem = CreateMenuGroup("Servers", new MenuItem[] {
                    SeperatorItem = new MenuItem("-"),
                    CreateMenuItem("Edit Servers...", new EventHandler(this.Config_Click)),
                    //CreateMenuItem("Show QRCode...", new EventHandler(this.QRCodeItem_Click)),
                }),
                SelectRandomItem = CreateMenuItem("Enable balance", new EventHandler(this.SelectRandomItem_Click)),
                CreateMenuItem("Global Settings...", new EventHandler(this.Setting_Click)),
                new MenuItem("-"),
                sameHostForSameTargetItem = CreateMenuItem("Same host for same address", new EventHandler(this.SelectSameHostForSameTargetItem_Click)),
                httpWhiteListItem = CreateMenuItem("Enable domain white list(http proxy only)", new EventHandler(this.HttpWhiteListItem_Click)),
                UpdateItem = CreateMenuItem("Update available", new EventHandler(this.UpdateItem_Clicked)),
                new MenuItem("-"),
                CreateMenuItem("Scan QRCode from Screen...", new EventHandler(this.ScanQRCodeItem_Click)),
                CreateMenuItem("Copy Address from clipboard...", new EventHandler(this.CopyAddress_Click)),
                new MenuItem("-"),
                CreateMenuItem("Server Statistic...", new EventHandler(this.ShowServerLogItem_Click)),
                CreateMenuItem("Disconnect Current", new EventHandler(this.DisconnectCurrent_Click)),
                CreateMenuGroup("Help", new MenuItem[] {
                    CreateMenuItem("Check Update", new EventHandler(this.CheckUpdate_Click)),
                    CreateMenuItem("Show Logs...", new EventHandler(this.ShowLogItem_Click)),
                    CreateMenuItem("About...", new EventHandler(this.AboutItem_Click)),
                    CreateMenuItem("Donate...", new EventHandler(this.DonateItem_Click)),
                }),
                new MenuItem("-"),
                CreateMenuItem("Quit", new EventHandler(this.Quit_Click))
            });
            this.UpdateItem.Visible = false;
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void controller_ToggleModeChanged(object sender, EventArgs e)
        {
            Configuration config = controller.GetConfiguration();
            enableItem.Checked = config.sysProxyMode == (int)ProxyMode.NoModify;
            PACModeItem.Checked = config.sysProxyMode == (int)ProxyMode.Pac;
            globalModeItem.Checked = config.sysProxyMode == (int)ProxyMode.Global;
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
            GFWListUpdater updater = (GFWListUpdater)sender;
            ShowBalloonTip(I18N.GetString("Failed to update PAC file"), e.GetException().Message, ToolTipIcon.Error, 5000);
            Logging.LogUsefulException(e.GetException());
        }

        void controller_UpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            GFWListUpdater updater = (GFWListUpdater)sender;
            string result = e.Success ?
                (updater.update_type <= 1 ? I18N.GetString("PAC updated") : I18N.GetString("Domain white list list updated"))
                : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
            ShowBalloonTip(I18N.GetString("Shadowsocks"), result, ToolTipIcon.Info, 1000);

            if (updater.update_type == 8)
            {
                controller.ToggleBypass(httpWhiteListItem.Checked);
            }
        }

        void updateChecker_NewVersionFound(object sender, EventArgs e)
        {
            if (updateChecker.LatestVersionNumber == null || updateChecker.LatestVersionNumber.Length == 0)
            {
                Logging.Log(LogLevel.Error, "connect to update server error");
            }
            else
            {
                if (!this.UpdateItem.Visible)
                {
                    ShowBalloonTip(String.Format(I18N.GetString("{0} {1} Update Found"), UpdateChecker.Name, updateChecker.LatestVersionNumber),
                        I18N.GetString("Click menu to download"), ToolTipIcon.Info, 5000);
                    _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;

                    timerDelayCheckUpdate.Elapsed -= timer_Elapsed;
                    timerDelayCheckUpdate.Stop();
                    timerDelayCheckUpdate = null;
                }
                this.UpdateItem.Visible = true;
                this.UpdateItem.Text = String.Format(I18N.GetString("New version {0} {1} available"), UpdateChecker.Name, updateChecker.LatestVersionNumber);
            }
        }

        void UpdateItem_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(updateChecker.LatestVersionURL);
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(updateChecker.LatestVersionURL);
            _notifyIcon.BalloonTipClicked -= notifyIcon1_BalloonTipClicked;
        }

        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetConfiguration();
            UpdateServersMenu();
            enableItem.Checked = config.sysProxyMode == (int)ProxyMode.NoModify;
            PACModeItem.Checked = config.sysProxyMode == (int)ProxyMode.Pac;
            globalModeItem.Checked = config.sysProxyMode == (int)ProxyMode.Global;
            SelectRandomItem.Checked = config.random;
            sameHostForSameTargetItem.Checked = config.sameHostForSameTarget;
            httpWhiteListItem.Checked = config.bypassWhiteList;
        }

        private void UpdateServersMenu()
        {
            var items = ServersItem.MenuItems;
            while (items[0] != SeperatorItem)
            {
                items.RemoveAt(0);
            }

            Configuration configuration = controller.GetConfiguration();
            for (int i = 0; i < configuration.configs.Count; i++)
            {
                Server server = configuration.configs[i];
                MenuItem item = new MenuItem(server.FriendlyName());
                item.Tag = i;
                item.Click += AServerItem_Click;
                items.Add(i, item);
            }

            if (configuration.index >= 0 && configuration.index < configuration.configs.Count)
            {
                items[configuration.index].Checked = true;
            }
        }

        private void ShowConfigForm(bool addNode)
        {
            if (configForm != null)
            {
                configForm.Activate();
            }
            else
            {
                configForm = new ConfigForm(controller, updateChecker, addNode ? -1 : -2);
                configForm.Show();
                configForm.Activate();
                configForm.BringToFront();
                configForm.FormClosed += configForm_FormClosed;
            }
        }

        private void ShowSettingForm()
        {
            if (settingsForm != null)
            {
                settingsForm.Activate();
            }
            else
            {
                settingsForm = new SettingsForm(controller);
                settingsForm.Show();
                settingsForm.Activate();
                settingsForm.BringToFront();
                settingsForm.FormClosed += settingsForm_FormClosed;
            }
        }

        private void ShowServerLogForm()
        {
            if (serverLogForm != null)
            {
                serverLogForm.Activate();
                serverLogForm.Update();
                if (serverLogForm.WindowState == FormWindowState.Minimized)
                {
                    serverLogForm.WindowState = FormWindowState.Normal;
                }
            }
            else
            {
                serverLogForm = new ServerLogForm(controller);
                serverLogForm.Show();
                serverLogForm.Activate();
                serverLogForm.BringToFront();
                serverLogForm.FormClosed += serverLogForm_FormClosed;
            }
        }

        void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            configForm = null;
            Util.Utils.ReleaseMemory();
        }

        void settingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            settingsForm = null;
            Util.Utils.ReleaseMemory();
        }

        void serverLogForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            serverLogForm = null;
        }

        private void Config_Click(object sender, EventArgs e)
        {
            ShowConfigForm(false);
        }

        private void Setting_Click(object sender, EventArgs e)
        {
            ShowSettingForm();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            controller.Stop();
            if (configForm != null)
            {
                configForm.Close();
                configForm = null;
            }
            if (serverLogForm != null)
            {
                serverLogForm.Close();
                serverLogForm = null;
            }
            if (timerDelayCheckUpdate != null)
            {
                timerDelayCheckUpdate.Elapsed -= timer_Elapsed;
                timerDelayCheckUpdate.Stop();
                timerDelayCheckUpdate = null;
            }
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/breakwa11/shadowsocks-rss");
        }

        private void DonateItem_Click(object sender, EventArgs e)
        {
            _notifyIcon.BalloonTipTitle = I18N.GetString("Why donate?");
            _notifyIcon.BalloonTipText = I18N.GetString("You can not donate!");
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(0);
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        private void notifyIcon1_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                GetAsyncKeyState(Keys.ShiftKey);
                if (GetAsyncKeyState(Keys.ShiftKey) < 0)
                {
                    ShowSettingForm();
                }
                else
                {
                    ShowConfigForm(false);
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                ShowServerLogForm();
            }
        }

        private void EnableItem_Click(object sender, EventArgs e)
        {
            controller.ToggleMode(0);
        }

        private void GlobalModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleMode(2);
        }

        private void PACModeItem_Click(object sender, EventArgs e)
        {
            controller.ToggleMode(1);
        }

        private void SelectRandomItem_Click(object sender, EventArgs e)
        {
            SelectRandomItem.Checked = !SelectRandomItem.Checked;
            controller.ToggleSelectRandom(SelectRandomItem.Checked);
        }

        private void SelectSameHostForSameTargetItem_Click(object sender, EventArgs e)
        {
            sameHostForSameTargetItem.Checked = !sameHostForSameTargetItem.Checked;
            controller.ToggleSameHostForSameTargetRandom(sameHostForSameTargetItem.Checked);
        }

        private void HttpWhiteListItem_Click(object sender, EventArgs e)
        {
            httpWhiteListItem.Checked = !httpWhiteListItem.Checked;
            if (httpWhiteListItem.Checked)
            {
                string bypass_path = Path.Combine(System.Windows.Forms.Application.StartupPath, PACServer.BYPASS_FILE);
                if (!File.Exists(bypass_path))
                {
                    controller.UpdateBypassListFromDefault();
                    return;
                }
            }
            controller.ToggleBypass(httpWhiteListItem.Checked);
        }

        private void EditPACFileItem_Click(object sender, EventArgs e)
        {
            controller.TouchPACFile();
        }

        private void UpdatePACFromGFWListItem_Click(object sender, EventArgs e)
        {
            controller.UpdatePACFromGFWList();
        }

        private void UpdatePACFromLanIPListItem_Click(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/breakwa11/gfw_whitelist/master/ssr/ss_lanip.pac");
        }

        private void UpdatePACFromCNWhiteListItem_Click(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/breakwa11/gfw_whitelist/master/ssr/ss_white.pac");
        }

        private void UpdatePACFromCNOnlyListItem_Click(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/breakwa11/gfw_whitelist/master/ssr/ss_r_white.pac");
        }

        private void UpdatePACFromCNIPListItem_Click(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/breakwa11/gfw_whitelist/master/ssr/ss_cnip.pac");
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

        private void CheckUpdate_Click(object sender, EventArgs e)
        {
            updateChecker.CheckUpdate(controller.GetCurrentConfiguration());
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", Logging.LogFile);
                return;
            }
            catch
            {
            }
            try
            {
                string argument = "/n" + ",/select," + Logging.LogFile;
                System.Diagnostics.Process.Start("explorer.exe", argument);
                return;
            }
            catch
            {
                _notifyIcon.BalloonTipTitle = "Show log failed";
                _notifyIcon.BalloonTipText = "try open the 'temp' directory by yourself";
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                _notifyIcon.ShowBalloonTip(0);
            }
        }
        private void ShowServerLogItem_Click(object sender, EventArgs e)
        {
            ShowServerLogForm();
        }

        private void DisconnectCurrent_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            for (int id = 0; id < config.configs.Count; ++id)
            {
                Server server = config.configs[id];
                server.GetConnections().CloseAll();
            }
        }

        private void CopyAddress_Click(object sender, EventArgs e)
        {
            IDataObject iData = Clipboard.GetDataObject();
            if (iData.GetDataPresent(DataFormats.Text))
            {
                string[] urls = ((string)iData.GetData(DataFormats.Text)).Split(new string[] { "\r", "\n", "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string url in urls)
                {
                    controller.AddServerBySSURL(url);
                }
                ShowConfigForm(true);
            }
        }

        private void QRCodeItem_Click(object sender, EventArgs e)
        {
            QRCodeForm qrCodeForm = new QRCodeForm(controller.GetSSLinkForCurrentServer());
            //qrCodeForm.Icon = this.Icon;
            // TODO
            qrCodeForm.Show();
        }

        private bool ScanQRCode(Screen screen, Bitmap fullImage, Rectangle cropRect, out string url, out Rectangle rect)
        {
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(fullImage, new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                                cropRect,
                                GraphicsUnit.Pixel);
            }
            var source = new BitmapLuminanceSource(target);
            var bitmap = new BinaryBitmap(new HybridBinarizer(source));
            QRCodeReader reader = new QRCodeReader();
            var result = reader.decode(bitmap);
            if (result != null)
            {
                url = result.Text;
                double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                foreach (ResultPoint point in result.ResultPoints)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }
                //rect = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                rect = new Rectangle(cropRect.Left + (int)minX, cropRect.Top + (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                return true;
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private bool ScanQRCodeStretch(Screen screen, Bitmap fullImage, Rectangle cropRect, double mul, out string url, out Rectangle rect)
        {
            Bitmap target = new Bitmap((int)(cropRect.Width * mul), (int)(cropRect.Height * mul));

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
                url = result.Text;
                double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                foreach (ResultPoint point in result.ResultPoints)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }
                //rect = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                rect = new Rectangle(cropRect.Left + (int)(minX / mul), cropRect.Top + (int)(minY / mul), (int)((maxX - minX) / mul), (int)((maxY - minY) / mul));
                return true;
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private Rectangle GetScanRect(int width, int height, int index, out double stretch)
        {
            stretch = 1;
            if (index < 5)
            {
                const int div = 5;
                int w = width * 3 / div;
                int h = height * 3 / div;
                Point[] pt = new Point[5] {
                    new Point(1, 1),

                    new Point(0, 0),
                    new Point(0, 2),
                    new Point(2, 0),
                    new Point(2, 2),
                };
                return new Rectangle(pt[index].X * width / div, pt[index].Y * height / div, w, h);
            }
            {
                const int base_index = 5;
                if (index < base_index + 6)
                {
                    double[] s = new double[] {
                        1,
                        2,
                        3,
                        4,
                        6,
                        8
                    };
                    stretch = 1 / s[index - base_index];
                    return new Rectangle(0, 0, width, height);
                }
            }
            {
                const int base_index = 11;
                if (index < base_index + 8)
                {
                    const int hdiv = 7;
                    const int vdiv = 5;
                    int w = width * 3 / hdiv;
                    int h = height * 3 / vdiv;
                    Point[] pt = new Point[8] {
                        new Point(1, 1),
                        new Point(3, 1),

                        new Point(0, 0),
                        new Point(0, 2),

                        new Point(2, 0),
                        new Point(2, 2),

                        new Point(4, 0),
                        new Point(4, 2),
                    };
                    return new Rectangle(pt[index - base_index].X * width / hdiv, pt[index - base_index].Y * height / vdiv, w, h);
                }
            }
            return new Rectangle(0, 0, 0, 0);
        }

        private void ScanScreenQRCode(bool ss_only)
        {
            Thread.Sleep(100);
            foreach (Screen screen in Screen.AllScreens)
            {
                Point screen_size = Util.Utils.GetScreenPhysicalSize();
                using (Bitmap fullImage = new Bitmap(screen_size.X,
                                                screen_size.Y))
                {
                    using (Graphics g = Graphics.FromImage(fullImage))
                    {
                        g.CopyFromScreen(screen.Bounds.X,
                                         screen.Bounds.Y,
                                         0, 0,
                                         fullImage.Size,
                                         CopyPixelOperation.SourceCopy);
                    }
                    bool decode_fail = false;
                    for (int i = 0; i < 100; i++)
                    {
                        double stretch;
                        Rectangle cropRect = GetScanRect(fullImage.Width, fullImage.Height, i, out stretch);
                        if (cropRect.Width == 0)
                            break;

                        string url;
                        Rectangle rect;
                        if (stretch == 1 ? ScanQRCode(screen, fullImage, cropRect, out url, out rect) : ScanQRCodeStretch(screen, fullImage, cropRect, stretch, out url, out rect))
                        {
                            var success = controller.AddServerBySSURL(url);
                            QRCodeSplashForm splash = new QRCodeSplashForm();
                            if (success)
                            {
                                splash.FormClosed += splash_FormClosed;
                            }
                            else if (!ss_only && (url.StartsWith("http://") || url.StartsWith("https://")))
                            {
                                _urlToOpen = url;
                                splash.FormClosed += openURLFromQRCode;
                            }
                            else
                            {
                                decode_fail = true;
                                continue;
                            }
                            splash.Location = new Point(screen.Bounds.X, screen.Bounds.Y);
                            double dpi = Screen.PrimaryScreen.Bounds.Width / (double)screen_size.X;
                            splash.TargetRect = new Rectangle(
                                (int)(rect.Left * dpi + screen.Bounds.X),
                                (int)(rect.Top * dpi + screen.Bounds.Y),
                                (int)(rect.Width * dpi),
                                (int)(rect.Height * dpi));
                            splash.Size = new Size(fullImage.Width, fullImage.Height);
                            splash.Show();
                            return;
                        }
                    }
                    if (decode_fail)
                    {
                        MessageBox.Show(I18N.GetString("Failed to decode QRCode"));
                        return;
                    }
                }
            }
            MessageBox.Show(I18N.GetString("No QRCode found. Try to zoom in or move it to the center of the screen."));
        }

        private void ScanQRCodeItem_Click(object sender, EventArgs e)
        {
            ScanScreenQRCode(true);
        }

        void splash_FormClosed(object sender, FormClosedEventArgs e)
        {
            ShowConfigForm(true);
        }

        void openURLFromQRCode(object sender, FormClosedEventArgs e)
        {
            Process.Start(_urlToOpen);
        }
    }
}
