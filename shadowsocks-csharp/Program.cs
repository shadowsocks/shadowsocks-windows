using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using Shadowsocks.View;

namespace Shadowsocks
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Utils.ReleaseMemory();
            using (var mutex = new Mutex(false, "Global\\" + "71981632-A427-497F-AB91-241CD227EC1F"))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!mutex.WaitOne(0, false))
                {
                    Process[] oldProcesses = Process.GetProcessesByName("Shadowsocks");
                    if (oldProcesses.Length > 0)
                    {
                    }
                    MessageBox.Show(Resources.MultiProcess);
                    return;
                }
                Directory.SetCurrentDirectory(Application.StartupPath);
#if !DEBUG
                Logging.AttatchToConsole();
#endif
                ShadowsocksController controller = new ShadowsocksController();

                MenuViewController.AttachMenu(controller);

                controller.Start();

                Application.Run();
            }
        }
    }
}
