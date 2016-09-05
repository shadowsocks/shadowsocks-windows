using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;
using System.Runtime.InteropServices;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    class KcptunProxyRunner
    {
        private Process _process;

        static KcptunProxyRunner()
        {
            try
            {
                FileManager.UncompressFile(Utils.GetTempPath("ss_kcptun.exe"), Resources.kcptun_exe);
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public bool HasExited()
        {
            if (_process == null)
                return true;
            return _process.HasExited;
        }

        public static void Kill()
        {
            Process[] existingPolipo = Process.GetProcessesByName("ss_kcptun");
            foreach (Process p in existingPolipo)
            {
                string str;
                try
                {
                    str = p.MainModule.FileName;
                }
                catch (Exception)
                {
                    continue;
                }
                if (str == Path.GetFullPath(Utils.GetTempPath("ss_kcptun.exe")))
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        public void Start(Configuration configuration)
        {
            Server server = configuration.GetCurrentServer();
            if (_process == null)
            {
                Kill();

                if (!server.enable_kcp)
                {
                    return;
                }

                _process = new Process();
                // Configure the process using the StartInfo properties.
                _process.StartInfo.FileName = Utils.GetTempPath("ss_kcptun.exe");
                _process.StartInfo.Arguments = "-r " + server.kcp_remote_addr + " -l \":" + server.server_port + "\" " + server.kcp_cli_params;
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.UseShellExecute = true;
                _process.StartInfo.CreateNoWindow = true;
                //_process.StartInfo.RedirectStandardOutput = true;
                //_process.StartInfo.RedirectStandardError = true;
                try
                {
                    _process.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void Stop()
        {
            if (_process != null)
            {
                try
                {
                    _process.Kill();
                    _process.WaitForExit();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                _process = null;
            }
            RefreshTrayArea();
        }

        private int GetFreePort()
        {
            int defaultPort = 60000;
            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

                List<int> usedPorts = new List<int>();
                foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                {
                    usedPorts.Add(endPoint.Port);
                }
                for (int nTry = 0; nTry < 1000; nTry++)
                {
                    int port = new Random().Next(10000, 65536);
                    if (!usedPorts.Contains(port))
                    {
                        return port;
                    }
                }
            }
            catch (Exception e)
            {
                // in case access denied
                Logging.LogUsefulException(e);
                return defaultPort;
            }
            throw new Exception("No free port found.");
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);


        public void RefreshTrayArea()
        {
            IntPtr systemTrayContainerHandle = FindWindow("Shell_TrayWnd", null);
            IntPtr systemTrayHandle = FindWindowEx(systemTrayContainerHandle, IntPtr.Zero, "TrayNotifyWnd", null);
            IntPtr sysPagerHandle = FindWindowEx(systemTrayHandle, IntPtr.Zero, "SysPager", null);
            IntPtr notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "Notification Area");
            if (notificationAreaHandle == IntPtr.Zero)
            {
                notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "User Promoted Notification Area");
                IntPtr notifyIconOverflowWindowHandle = FindWindow("NotifyIconOverflowWindow", null);
                IntPtr overflowNotificationAreaHandle = FindWindowEx(notifyIconOverflowWindowHandle, IntPtr.Zero, "ToolbarWindow32", "Overflow Notification Area");
                RefreshTrayArea(overflowNotificationAreaHandle);
            }
            RefreshTrayArea(notificationAreaHandle);
        }


        private static void RefreshTrayArea(IntPtr windowHandle)
        {
            const uint wmMousemove = 0x0200;
            RECT rect;
            GetClientRect(windowHandle, out rect);
            for (var x = 0; x < rect.right; x += 5)
                for (var y = 0; y < rect.bottom; y += 5)
                    SendMessage(windowHandle, wmMousemove, 0, (y << 16) + x);
        }
    }
}
