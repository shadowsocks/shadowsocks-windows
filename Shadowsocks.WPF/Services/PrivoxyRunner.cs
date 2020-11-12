using Shadowsocks.Net.Settings;
using Shadowsocks.WPF.Utils;
using Splat;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Shadowsocks.WPF.Services
{
    public class PrivoxyRunner : IEnableLogger
    {
        private static int _uid;
        private static string _uniqueConfigFile = "";
        private Process? _process;
        private int _runningPort;

        public PrivoxyRunner()
        {
            try
            {
                _uid = Utils.Utilities.WorkingDirectory.GetHashCode(); // Currently we use ss's StartupPath to identify different Privoxy instance.
                _uniqueConfigFile = $"privoxy_{_uid}.conf";

                FileManager.UncompressFile(Utils.Utilities.GetTempPath("ss_privoxy.exe"), Properties.Resources.privoxy_exe);
            }
            catch (IOException e)
            {
                this.Log().Error(e, "An error occurred while starting Privoxy.");
                _uniqueConfigFile = "";
            }
        }

        public int RunningPort => _runningPort;

        public void Start(NetSettings netSettings)
        {
            if (_process == null)
            {
                Process[] existingPrivoxy = Process.GetProcessesByName("ss_privoxy");
                foreach (Process p in existingPrivoxy.Where(IsChildProcess))
                {
                    KillProcess(p);
                }
                string privoxyConfig = Properties.Resources.privoxy_conf;
                _runningPort = GetFreePort(netSettings);
                privoxyConfig = privoxyConfig.Replace("__SOCKS_PORT__", netSettings.Socks5ListeningPort.ToString());
                privoxyConfig = privoxyConfig.Replace("__PRIVOXY_BIND_PORT__", _runningPort.ToString());
                privoxyConfig = privoxyConfig.Replace("__PRIVOXY_BIND_IP__", $"[{netSettings.Socks5ListeningAddress}]")
                    .Replace("__SOCKS_HOST__", "[::1]"); // TODO: make sure it's correct
                FileManager.ByteArrayToFile(Utils.Utilities.GetTempPath(_uniqueConfigFile), Encoding.UTF8.GetBytes(privoxyConfig));

                _process = new Process
                {
                    // Configure the process using the StartInfo properties.
                    StartInfo =
                    {
                        FileName = "ss_privoxy.exe",
                        Arguments = _uniqueConfigFile,
                        WorkingDirectory = Utils.Utilities.GetTempPath(),
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                _process.Start();
            }
        }

        public void Stop()
        {
            if (_process != null)
            {
                KillProcess(_process);
                _process.Dispose();
                _process = null;
            }
        }

        private void KillProcess(Process p)
        {
            try
            {
                p.CloseMainWindow();
                p.WaitForExit(100);
                if (!p.HasExited)
                {
                    p.Kill();
                    p.WaitForExit();
                }
            }
            catch (Exception e)
            {
                this.Log().Error(e, "An error occurred while stopping Privoxy.");
            }
        }

        /*
         * We won't like to kill other ss instances' ss_privoxy.exe.
         * This function will check whether the given process is created
         * by this process by checking the module path or command line.
         * 
         * Since it's required to put ss in different dirs to run muti instances,
         * different instance will create their unique "privoxy_UID.conf" where
         * UID is hash of ss's location.
         */

        private bool IsChildProcess(Process process)
        {
            try
            {
                /*
                 * Under PortableMode, we could identify it by the path of ss_privoxy.exe.
                 */
                var path = process.MainModule?.FileName;

                return Utils.Utilities.GetTempPath("ss_privoxy.exe").Equals(path);

            }
            catch (Exception ex)
            {
                /*
                 * Sometimes Process.GetProcessesByName will return some processes that
                 * are already dead, and that will cause exceptions here.
                 * We could simply ignore those exceptions.
                 */
                this.Log().Error(ex, "");
                return false;
            }
        }

        private int GetFreePort(NetSettings netSettings)
        {
            int defaultPort = 8123;
            try
            {
                // TCP stack please do me a favor
                TcpListener l = new TcpListener(IPAddress.Parse(netSettings.Socks5ListeningAddress), 0);
                l.Start();
                var port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
                return port;
            }
            catch (Exception e)
            {
                // in case access denied
                this.Log().Error(e, "");
                return defaultPort;
            }
        }
    }
}
