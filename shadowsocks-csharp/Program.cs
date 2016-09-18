using Shadowsocks.Controller;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
#if !_CONSOLE
using Shadowsocks.View;
#endif

namespace Shadowsocks
{
    static class Program
    {
        static ShadowsocksController _controller;
#if !_CONSOLE
        static MenuViewController _viewController;
#endif

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if !_CONSOLE
            using (Mutex mutex = new Mutex(false, "Global\\ShadowsocksR_" + Application.StartupPath.GetHashCode()))
            {
                Application.EnableVisualStyles();
                Application.ApplicationExit += Application_ApplicationExit;
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                Application.SetCompatibleTextRenderingDefault(false);

                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show(I18N.GetString("Find Shadowsocks icon in your notify tray.") + "\n" +
                        I18N.GetString("If you want to start multiple Shadowsocks, make a copy in another directory."),
                        I18N.GetString("ShadowsocksR is already running."));
                    return;
                }
#endif
                Directory.SetCurrentDirectory(Application.StartupPath);
//#if !DEBUG
                Logging.OpenLogFile();
//#endif
                _controller = new ShadowsocksController();

#if !_CONSOLE
                _viewController = new MenuViewController(_controller);
#endif

                _controller.Start();

#if !_CONSOLE
                //Util.Utils.ReleaseMemory();

                Application.Run();
            }
#else
            Console.ReadLine();
            _controller.Stop();
#endif
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    if (_controller != null)
                    {
                        System.Timers.Timer timer = new System.Timers.Timer(5 * 1000);
                        timer.Elapsed += Timer_Elapsed;
                        timer.AutoReset = false;
                        timer.Enabled = true;
                        timer.Start();
                    }
                    break;
                case PowerModes.Suspend:
                    _controller?.Stop();
                    break;
            }
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _controller?.Start();
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
            finally
            {
                try
                {
                    System.Timers.Timer timer = (System.Timers.Timer)sender;
                    timer.Enabled = false;
                    timer.Stop();
                    timer.Dispose();
                }
                catch (Exception ex)
                {
                    Logging.LogUsefulException(ex);
                }
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            _controller?.Stop();
            _controller = null;
        }
    }
}
