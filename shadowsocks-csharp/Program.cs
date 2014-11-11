using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Shadowsocks
{
    static class Program
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, "Global\\" + "71981632-A427-497F-AB91-241CD227EC1F"))
            {
                if (!mutex.WaitOne(0, false))
                {
                    Process[] oldProcesses = Process.GetProcessesByName("Shadowsocks");
                    if (oldProcesses.Length > 0)
                    {
                        Process oldProcess = oldProcesses[0];
                    }
                    MessageBox.Show("Shadowsocks is already running.\n\nFind Shadowsocks icon in your notify tray.");
                    return;
                }
                string tempPath = Path.GetTempPath();
                string dllPath = tempPath + "/libeay32.dll";
                try
                {
                    FileManager.UncompressFile(dllPath, Resources.libeay32_dll);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.ToString());
                }
                //LoadLibrary(dllPath);

#if !DEBUG
                Logging.OpenLogFile();
#endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ShadowsocksController controller = new ShadowsocksController();

                // TODO run without a main form to save RAM
                Application.Run(new ConfigForm(controller));
            }
        }
    }
}
