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
    class PolipoRunner
    {
        private Process _process;
        private static string temppath;
        private int _runningPort;

        static PolipoRunner()
        {
            temppath = Utils.GetTempPath();
            try
            {
                FileManager.UncompressFile(temppath + "/ss_privoxy.exe", Resources.privoxy_exe);
                FileManager.UncompressFile(temppath + "/mgwz.dll", Resources.mgwz_dll);
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public int RunningPort
        {
            get
            {
                return _runningPort;
            }
        }

        public void Start(Configuration configuration)
        {
            Server server = configuration.GetCurrentServer();
            if (_process == null)
            {
                Process[] existingPolipo = Process.GetProcessesByName("ss_privoxy");
                foreach (Process p in existingPolipo)
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
                string polipoConfig = Resources.privoxy_conf;
                _runningPort = this.GetFreePort();
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", configuration.localPort.ToString());
                polipoConfig = polipoConfig.Replace("__POLIPO_BIND_PORT__", _runningPort.ToString());
                polipoConfig = polipoConfig.Replace("__POLIPO_BIND_IP__", configuration.shareOverLan ? "0.0.0.0" : "127.0.0.1");
                FileManager.ByteArrayToFile(temppath + "/privoxy.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));

                if (!(temppath.EndsWith("\\") || temppath.EndsWith("/"))) {
                    temppath = temppath + "\\";
                }
                _process = new Process();
                // Configure the process using the StartInfo properties.
                _process.StartInfo.FileName = temppath + "ss_privoxy.exe";
                _process.StartInfo.Arguments = " \"" + temppath + "privoxy.conf\"";
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.UseShellExecute = true;
                _process.StartInfo.CreateNoWindow = true;
                //_process.StartInfo.RedirectStandardOutput = true;
                //_process.StartInfo.RedirectStandardError = true;
                _process.Start();
            }
            RefreshTrayArea();
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
            int defaultPort = 8123;
            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

                List<int> usedPorts = new List<int>();
                foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                {
                    usedPorts.Add(endPoint.Port);
                }
                for (int port = defaultPort; port <= 65535; port++)
                {
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
