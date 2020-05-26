using Microsoft.Win32;
using NLog;
using Shadowsocks.Controller;
using Shadowsocks.Controller.Hotkeys;
using Shadowsocks.Util;
using Shadowsocks.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shadowsocks
{
    internal static class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static ShadowsocksController MainController { get; private set; }
        public static MenuViewController MenuController { get; private set; }
        public static string[] Args { get; private set; }
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Application.StartupPath);
            // todo: initialize the NLog configuartion
            Model.NLogConfig.TouchAndApplyNLogConfig();

            // .NET Framework 4.7.2 on Win7 compatibility
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // store args for further use
            Args = args;
            // Check OS since we are using dual-mode socket
            if (!Utils.IsWinVistaOrHigher())
            {
                MessageBox.Show(I18N.GetString("Unsupported operating system, use Windows Vista at least."),
                "Shadowsocks Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check .NET Framework version
            if (!Utils.IsSupportedRuntimeVersion())
            {
                if (DialogResult.OK == MessageBox.Show(I18N.GetString("Unsupported .NET Framework, please update to {0} or later.", "4.7.2"),
                "Shadowsocks Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error))
                {
                    Process.Start("https://dotnet.microsoft.com/download/dotnet-framework/net472");
                }
                return;
            }
            string pipename = $"Shadowsocks\\{Application.StartupPath.GetHashCode()}";

            string addedUrl = null;

            using (NamedPipeClientStream pipe = new NamedPipeClientStream(pipename))
            {
                bool pipeExist = false;
                try
                {
                    pipe.Connect(10);
                    pipeExist = true;
                }
                catch (TimeoutException)
                {
                    pipeExist = false;
                }

                // TODO: switch to better argv parser when it's getting complicate
                List<string> alist = Args.ToList();
                // check --open-url param
                int urlidx = alist.IndexOf("--open-url") + 1;
                if (urlidx > 0)
                {
                    if (Args.Length <= urlidx)
                    {
                        return;
                    }

                    // --open-url exist, and no other instance, add it later
                    if (!pipeExist)
                    {
                        addedUrl = Args[urlidx];
                    }
                    // has other instance, send url via pipe then exit
                    else
                    {
                        byte[] b = Encoding.UTF8.GetBytes(Args[urlidx]);
                        byte[] opAddUrl = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1));
                        byte[] blen = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(b.Length));
                        pipe.Write(opAddUrl, 0, 4); // opcode addurl
                        pipe.Write(blen, 0, 4);
                        pipe.Write(b, 0, b.Length);
                        pipe.Close();
                        return;
                    }
                }
                // has another instance, and no need to communicate with it return
                else if (pipeExist)
                {
                    Process[] oldProcesses = Process.GetProcessesByName("Shadowsocks");
                    if (oldProcesses.Length > 0)
                    {
                        Process oldProcess = oldProcesses[0];
                    }
                    MessageBox.Show(I18N.GetString("Find Shadowsocks icon in your notify tray.")
                        + Environment.NewLine
                        + I18N.GetString("If you want to start multiple Shadowsocks, make a copy in another directory."),
                        I18N.GetString("Shadowsocks is already running."));
                    return;
                }
            }

            Utils.ReleaseMemory(true);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // handle UI exceptions
            Application.ThreadException += Application_ThreadException;
            // handle non-UI exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ApplicationExit += Application_ApplicationExit;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AutoStartup.RegisterForRestart(true);

            Directory.SetCurrentDirectory(Application.StartupPath);

#if DEBUG
            // truncate privoxy log file while debugging
            string privoxyLogFilename = Utils.GetTempPath("privoxy.log");
            if (File.Exists(privoxyLogFilename))
                using (new FileStream(privoxyLogFilename, FileMode.Truncate)) { }
#endif
            MainController = new ShadowsocksController();
            MenuController = new MenuViewController(MainController);

            HotKeys.Init(MainController);
            MainController.Start();

            NamedPipeServer namedPipeServer = new NamedPipeServer();
            Task.Run(() => namedPipeServer.Run(pipename));
            namedPipeServer.AddUrlRequested += (_1, e) => MainController.AskAddServerBySSURL(e.Url);
            if (!addedUrl.IsNullOrEmpty())
            {
                MainController.AskAddServerBySSURL(addedUrl);
            }
            Application.Run();
        }

        private static int exited = 0;
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref exited) == 1)
            {
                string errMsg = e.ExceptionObject.ToString();
                logger.Error(errMsg);
                MessageBox.Show(
                    $"{I18N.GetString("Unexpected error, shadowsocks will exit. Please report to")} https://github.com/shadowsocks/shadowsocks-windows/issues {Environment.NewLine}{errMsg}",
                    "Shadowsocks non-UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref exited) == 1)
            {
                string errorMsg = $"Exception Detail: {Environment.NewLine}{e.Exception}";
                logger.Error(errorMsg);
                MessageBox.Show(
                    $"{I18N.GetString("Unexpected error, shadowsocks will exit. Please report to")} https://github.com/shadowsocks/shadowsocks-windows/issues {Environment.NewLine}{errorMsg}",
                    "Shadowsocks UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    logger.Info("os wake up");
                    if (MainController != null)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(10 * 1000);
                            try
                            {
                                MainController.Start(false);
                                logger.Info("controller started");
                            }
                            catch (Exception ex)
                            {
                                logger.LogUsefulException(ex);
                            }
                        });
                    }
                    break;
                case PowerModes.Suspend:
                    if (MainController != null)
                    {
                        MainController.Stop();
                        logger.Info("controller stopped");
                    }
                    logger.Info("os suspend");
                    break;
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            // detach static event handlers
            Application.ApplicationExit -= Application_ApplicationExit;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            Application.ThreadException -= Application_ThreadException;
            HotKeys.Destroy();
            if (MainController != null)
            {
                MainController.Stop();
                MainController = null;
            }
        }
    }
}
