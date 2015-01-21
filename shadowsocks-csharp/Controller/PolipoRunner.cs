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

namespace Shadowsocks.Controller
{
    class PolipoRunner
    {
        private Process _process;
        private static string temppath;
        private int _runningPort;

        static PolipoRunner()
        {
            temppath = Path.GetTempPath();
            try
            {
                FileManager.UncompressFile(temppath + "/ss_polipo.exe", Resources.polipo_exe);
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
                Process[] existingPolipo = Process.GetProcessesByName("ss_polipo");
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
                string polipoConfig = Resources.polipo_config;
                _runningPort = this.GetFreePort();
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", configuration.localPort.ToString());
                polipoConfig = polipoConfig.Replace("__POLIPO_BIND_PORT__", _runningPort.ToString());
                polipoConfig = polipoConfig.Replace("__POLIPO_BIND_IP__", configuration.shareOverLan ? "0.0.0.0" : "127.0.0.1");
                FileManager.ByteArrayToFile(temppath + "/polipo.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));

                _process = new Process();
                // Configure the process using the StartInfo properties.
                _process.StartInfo.FileName = temppath + "/ss_polipo.exe";
                _process.StartInfo.Arguments = "-c \"" + temppath + "/polipo.conf\"";
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.UseShellExecute = true;
                _process.StartInfo.CreateNoWindow = true;
                //_process.StartInfo.RedirectStandardOutput = true;
                //_process.StartInfo.RedirectStandardError = true;
                _process.Start();
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
    }
}
