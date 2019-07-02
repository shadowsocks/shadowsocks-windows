using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shadowsocks.Encryption;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Threading.Tasks;

namespace Shadowsocks.Controller
{
    public class PACServer : Listener.Service
    {
        public const string PAC_FILE = "pac.txt";
        public const string USER_RULE_FILE = "user-rule.txt";
        public const string USER_ABP_FILE = "abp.txt";

        private string PacSecret { get; set; } = "";

        public string PacUrl { get; private set; } = "";

        FileSystemWatcher PACFileWatcher;
        FileSystemWatcher UserRuleFileWatcher;
        private Configuration _config;

        public event EventHandler PACFileChanged;
        public event EventHandler UserRuleFileChanged;

        public PACServer()
        {
            this.WatchPacFile();
            this.WatchUserRuleFile();
        }

        public void UpdateConfiguration(Configuration config)
        {
            this._config = config;

            if (config.secureLocalPac)
            {
                var rd = new byte[32];
                RNG.GetBytes(rd);
                PacSecret = $"&secret={Convert.ToBase64String(rd)}";
            }
            else
            {
                PacSecret = "";
            }

            PacUrl = $"http://{config.localHost}:{config.localPort}/pac?t={GetTimestamp(DateTime.Now)}{PacSecret}";
        }


        private static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssfff");
        }

        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
            {
                return false;
            }
            try
            {
                string request = Encoding.UTF8.GetString(firstPacket, 0, length);
                string[] lines = request.Split('\r', '\n');
                bool hostMatch = false, pathMatch = false, useSocks = false;
                bool secretMatch = PacSecret.IsNullOrEmpty();
                foreach (string line in lines)
                {
                    string[] kv = line.Split(new char[] { ':' }, 2);
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "Host")
                        {
                            if (kv[1].Trim() == ((IPEndPoint)socket.LocalEndPoint).ToString())
                            {
                                hostMatch = true;
                            }
                        }
                        //else if (kv[0] == "User-Agent")
                        //{
                        //    // we need to drop connections when changing servers
                        //    if (kv[1].IndexOf("Chrome") >= 0)
                        //    {
                        //        useSocks = true;
                        //    }
                        //}
                    }
                    else if (kv.Length == 1)
                    {
                        if (line.IndexOf("pac", StringComparison.Ordinal) >= 0)
                        {
                            pathMatch = true;
                        }
                        if (!secretMatch)
                        {
                            if (line.IndexOf(PacSecret, StringComparison.Ordinal) >= 0)
                            {
                                secretMatch = true;
                            }
                        }
                    }
                }
                if (hostMatch && pathMatch)
                {
                    if (!secretMatch)
                    {
                        socket.Close(); // Close immediately
                    }
                    else
                    {
                        SendResponse(socket, useSocks);
                    }
                    return true;
                }
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public string TouchPACFile()
        {
            if (File.Exists(PAC_FILE))
            {
                return PAC_FILE;
            }
            else
            {
                FileManager.UncompressFile(PAC_FILE, Resources.proxy_pac_txt);
                return PAC_FILE;
            }
        }

        internal string TouchUserRuleFile()
        {
            if (File.Exists(USER_RULE_FILE))
            {
                return USER_RULE_FILE;
            }
            else
            {
                File.WriteAllText(USER_RULE_FILE, Resources.user_rule);
                return USER_RULE_FILE;
            }
        }

        private string GetPACContent()
        {
            if (File.Exists(PAC_FILE))
            {
                return File.ReadAllText(PAC_FILE, Encoding.UTF8);
            }
            else
            {
                return Utils.UnGzip(Resources.proxy_pac_txt);
            }
        }

        public void SendResponse(Socket socket, bool useSocks)
        {
            try
            {
                IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;

                string proxy = GetPACAddress(localEndPoint, useSocks);

                string pacContent = GetPACContent().Replace("__PROXY__", proxy);

                string responseHead = String.Format(@"HTTP/1.1 200 OK
Server: Shadowsocks
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: {0}
Connection: Close

", Encoding.UTF8.GetBytes(pacContent).Length);
                byte[] response = Encoding.UTF8.GetBytes(responseHead + pacContent);
                socket.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), socket);
                Utils.ReleaseMemory(true);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                socket.Close();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            try
            {
                conn.Shutdown(SocketShutdown.Send);
            }
            catch
            { }
        }

        private void WatchPacFile()
        {
            PACFileWatcher?.Dispose();
            PACFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            PACFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            PACFileWatcher.Filter = PAC_FILE;
            PACFileWatcher.Changed += PACFileWatcher_Changed;
            PACFileWatcher.Created += PACFileWatcher_Changed;
            PACFileWatcher.Deleted += PACFileWatcher_Changed;
            PACFileWatcher.Renamed += PACFileWatcher_Changed;
            PACFileWatcher.EnableRaisingEvents = true;
        }

        private void WatchUserRuleFile()
        {
            UserRuleFileWatcher?.Dispose();
            UserRuleFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            UserRuleFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            UserRuleFileWatcher.Filter = USER_RULE_FILE;
            UserRuleFileWatcher.Changed += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Created += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Deleted += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Renamed += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.EnableRaisingEvents = true;
        }

        #region FileSystemWatcher.OnChanged()
        // FileSystemWatcher Changed event is raised twice
        // http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
        // Add a short delay to avoid raise event twice in a short period
        private void PACFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (PACFileChanged != null)
            {
                Logging.Info($"Detected: PAC file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
                Task.Factory.StartNew(() =>
                {
                    ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                    System.Threading.Thread.Sleep(10);
                    PACFileChanged(this, new EventArgs());
                    ((FileSystemWatcher)sender).EnableRaisingEvents = true;
                });
            }
        }

        private void UserRuleFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (UserRuleFileChanged != null)
            {
                Logging.Info($"Detected: User Rule file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
                Task.Factory.StartNew(() =>
                {
                    ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                    System.Threading.Thread.Sleep(10);
                    UserRuleFileChanged(this, new EventArgs());
                    ((FileSystemWatcher)sender).EnableRaisingEvents = true;
                });
            }
        }
        #endregion

        private string GetPACAddress(IPEndPoint localEndPoint, bool useSocks)
        {
            return localEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                ? $"{(useSocks ? "SOCKS5" : "PROXY")} [{localEndPoint.Address}]:{_config.localPort};"
                : $"{(useSocks ? "SOCKS5" : "PROXY")} {localEndPoint.Address}:{_config.localPort};";
        }
    }
}
