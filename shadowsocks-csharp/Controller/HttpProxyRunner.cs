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
using System.Windows.Forms;

namespace Shadowsocks.Controller
{
    class HttpProxyRunner
    {
        private Process _process;
        private static string runningPath;
        private int _runningPort;
        private static string _subPath = @"temp";
        private static string _exeNameNoExt = @"/ssr_privoxy";
        private static string _exeName = @"/ssr_privoxy.exe";

        static HttpProxyRunner()
        {
            runningPath = Path.Combine(System.Windows.Forms.Application.StartupPath, _subPath);
            _exeNameNoExt = System.IO.Path.GetFileNameWithoutExtension(Util.Utils.GetExecutablePath());
            _exeName = @"/" + _exeNameNoExt + @".exe";
            if (!Directory.Exists(runningPath))
            {
                Directory.CreateDirectory(runningPath);
            }
            Kill();
            try
            {
                FileManager.UncompressFile(runningPath + _exeName, Resources.privoxy_exe);
                FileManager.UncompressFile(runningPath + "/mgwz.dll", Resources.mgwz_dll);
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

        public bool HasExited()
        {
            if (_process == null)
                return true;
            return _process.HasExited;
        }

        public static void Kill()
        {
            Process[] existingPolipo = Process.GetProcessesByName(_exeNameNoExt);
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
                if (str == Path.GetFullPath(runningPath + _exeName))
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
            if (_process == null)
            {
                Kill();
                string polipoConfig = Resources.privoxy_conf;
                bool bypass = configuration.bypassWhiteList;
                _runningPort = this.GetFreePort();
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", configuration.localPort.ToString());
                polipoConfig = polipoConfig.Replace("__PRIVOXY_BIND_PORT__", _runningPort.ToString());
                polipoConfig = polipoConfig.Replace("__PRIVOXY_BIND_IP__", "127.0.0.1");
                polipoConfig = polipoConfig.Replace("__BYPASS_ACTION__", "actionsfile " + _subPath + "/bypass.action");
                FileManager.ByteArrayToFile(runningPath + "/privoxy.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));

                //string bypassConfig = "{+forward-override{forward .}}\n0.[0-9]*.[0-9]*.[0-9]*/\n10.[0-9]*.[0-9]*.[0-9]*/\n127.[0-9]*.[0-9]*.[0-9]*/\n192.168.[0-9]*.[0-9]*/\n172.1[6-9].[0-9]*.[0-9]*/\n172.2[0-9].[0-9]*.[0-9]*/\n172.3[0-1].[0-9]*.[0-9]*/\n169.254.[0-9]*.[0-9]*/\n::1/\nfc00::/\nfe80::/\nlocalhost/\n";
                string bypassConfig = "{+forward-override{forward .}}\n";
                if (bypass)
                {
                    string bypass_path = Path.Combine(System.Windows.Forms.Application.StartupPath, PACServer.BYPASS_FILE);
                    if (File.Exists(bypass_path))
                    {
                        bypassConfig += File.ReadAllText(bypass_path, Encoding.UTF8);
                    }
                }
                FileManager.ByteArrayToFile(runningPath + "/bypass.action", System.Text.Encoding.UTF8.GetBytes(bypassConfig));

                Restart();
            }
        }

        public void Restart()
        {
            _process = new Process();
            // Configure the process using the StartInfo properties.
            _process.StartInfo.FileName = runningPath + _exeName;
            _process.StartInfo.Arguments = " \"" + runningPath + "/privoxy.conf\"";
            _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
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
                finally
                {
                    _process = null;
                }
            }
        }

        private int GetFreePort()
        {
            int defaultPort = 60000;
            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();
                Random random = new Random(Util.Utils.GetExecutablePath().GetHashCode() ^ (int)DateTime.Now.Ticks);

                List<int> usedPorts = new List<int>();
                foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
                {
                    usedPorts.Add(endPoint.Port);
                }

                for (int nTry = 0; nTry < 1000; nTry++)
                {
                    int port = random.Next(10000, 65536);
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
    }
}
