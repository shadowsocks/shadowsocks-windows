using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Shadowsocks
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg == "-setfips")
                {
                    Controller.SystemProxy.SetFIPS(0);
                    return;
                }
            }
            if (Controller.SystemProxy.GetFIPS() > 0)
            {
                DialogResult result =  MessageBox.Show("FIPS must be shutdown. Do you want to shut it down automatically?",
                    UpdateChecker.Name + ": FIPS setting",
                    MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (!Controller.SystemProxy.SetFIPS(0))
                    {
                        Process process = null;
                        ProcessStartInfo processInfo = new ProcessStartInfo();
                        processInfo.Verb = "runas";
                        processInfo.FileName = Application.ExecutablePath;
                        processInfo.Arguments = "-setfips";
                        try
                        {
                            process = Process.Start(processInfo);
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            MessageBox.Show("Not permit to modify the setting\r\n"
                                + "You can try to run this program by administrator or you can do it your self:\r\n\r\n"
                                + "Set \"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa\\FipsAlgorithmPolicy\\Enabled\" to 0",
                                UpdateChecker.Name + ": FIPS setting");
                            return;
                        }
                        if (process != null)
                        {
                            process.WaitForExit();
                        }
                        process.Close();
                    }
                }
            }
            using (Mutex mutex = new Mutex(false, "Global\\ShadowsocksR_" + Application.StartupPath.GetHashCode()))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show(I18N.GetString("Find Shadowsocks icon in your notify tray.") + "\n" +
                        I18N.GetString("If you want to start multiple Shadowsocks, make a copy in another directory."),
                        I18N.GetString("ShadowsocksR is already running."));
                    return;
                }
                Directory.SetCurrentDirectory(Application.StartupPath);
//#if !DEBUG
                Logging.OpenLogFile();
//#endif
                ShadowsocksController controller = new ShadowsocksController();

                MenuViewController viewController = new MenuViewController(controller);

                controller.Start();

                Util.Utils.ReleaseMemory();

                Application.Run();
            }
        }
    }
}
