using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    class PACServer : Listener.Service
    {
        public static readonly string PAC_FILE = "pac.txt";
        public static readonly string USER_RULE_FILE = "user-rule.txt";
        public static readonly string USER_ABP_FILE = "abp.txt";

        private FileSystemWatcher _pacFileWatcher;
        private FileSystemWatcher _userRuleFileWatcher;
        private Configuration _config;

        public event EventHandler PACFileChanged;
        public event EventHandler UserRuleFileChanged;

        public PACServer()
        {
            WatchPacFile();
            WatchUserRuleFile();
        }

        public void UpdateConfiguration(Configuration config)
        {
            _config = config;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket, object state)
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
                        if (line.IndexOf("pac") >= 0)
                        {
                            pathMatch = true;
                        }
                    }
                }
                if (hostMatch && pathMatch)
                {
                    SendResponse(firstPacket, length, socket, useSocks);
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
            if (!File.Exists(PAC_FILE))
                FileManager.UncompressFile(PAC_FILE, Resources.proxy_pac_txt);
            return PAC_FILE;
        }

        internal string TouchUserRuleFile()
        {
            if (!File.Exists(USER_RULE_FILE))
                Utils.WriteAllText(USER_RULE_FILE, Resources.user_rule);
            return USER_RULE_FILE;
        }

        private string GetPACContent()
        {
            Thread.Sleep(50);   // TODO: Aviod IO exception. It's a dirty hack, clean it someday later.
            var result = File.Exists(PAC_FILE)
                       ? File.ReadAllText(PAC_FILE, Encoding.UTF8)
                       : Utils.UnGzip(Resources.proxy_pac_txt);
            return result;
        }

        public void SendResponse(byte[] firstPacket, int length, Socket socket, bool useSocks)
        {
            try
            {
                IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;
                var proxy = GetPACAddress(firstPacket, length, localEndPoint, useSocks);
                var pacContent = GetPACContent().Replace("__PROXY__", proxy);
                var responseText = "HTTP/1.1 200 OK" + Environment.NewLine
                                 + "Server: Shadowsocks" + Environment.NewLine
                                 + "Content-Type: application/x-ns-proxy-autoconfig" + Environment.NewLine
                                 + $"Content-Length: {Encoding.UTF8.GetBytes(pacContent).Length}" + Environment.NewLine
                                 + "Connection: Close" + Environment.NewLine
                                 + Environment.NewLine
                                 + pacContent;
                var responseBytes = Encoding.UTF8.GetBytes(responseText);
                socket.BeginSend(responseBytes, 0, responseBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
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
            _pacFileWatcher?.Dispose();
            _pacFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            _pacFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _pacFileWatcher.Filter = PAC_FILE;
            _pacFileWatcher.Changed += PACFileWatcher_Changed;
            _pacFileWatcher.Created += PACFileWatcher_Changed;
            _pacFileWatcher.Deleted += PACFileWatcher_Changed;
            _pacFileWatcher.Renamed += PACFileWatcher_Changed;
            _pacFileWatcher.EnableRaisingEvents = true;
        }

        private void WatchUserRuleFile()
        {
            _userRuleFileWatcher?.Dispose();
            _userRuleFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            _userRuleFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _userRuleFileWatcher.Filter = USER_RULE_FILE;
            _userRuleFileWatcher.Changed += UserRuleFileWatcher_Changed;
            _userRuleFileWatcher.Created += UserRuleFileWatcher_Changed;
            _userRuleFileWatcher.Deleted += UserRuleFileWatcher_Changed;
            _userRuleFileWatcher.Renamed += UserRuleFileWatcher_Changed;
            _userRuleFileWatcher.EnableRaisingEvents = true;
        }

        #region FileSystemWatcher.OnChanged()
        // FileSystemWatcher Changed event is raised twice
        // http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
        private static Hashtable fileChangedTime = new Hashtable();

        private void PACFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath.ToString();
            string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();

            // if there is no path info stored yet or stored path has different time of write then the one now is inspected
            if (!fileChangedTime.ContainsKey(path) || fileChangedTime[path].ToString() != currentLastWriteTime)
            {
                if (PACFileChanged != null)
                {
                    Logging.Info($"Detected: PAC file '{e.Name}' was {e.ChangeType.ToString().ToLower()}. Reload PAC server.");
                    PACFileChanged(this, new EventArgs());
                }

                // lastly we update the last write time in the hashtable
                fileChangedTime[path] = currentLastWriteTime;
            }
        }

        private void UserRuleFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath.ToString();
            string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();

            // if there is no path info stored yet or stored path has different time of write then the one now is inspected
            if (!fileChangedTime.ContainsKey(path) || fileChangedTime[path].ToString() != currentLastWriteTime)
            {
                if (UserRuleFileChanged != null)
                {
                    Logging.Info($"Detected: User Rule file '{e.Name}' was {e.ChangeType.ToString().ToLower()}. Reload PAC server.");
                    UserRuleFileChanged(this, new EventArgs());
                }
                // lastly we update the last write time in the hashtable
                fileChangedTime[path] = currentLastWriteTime;
            }
        }
        #endregion

        private string GetPACAddress(byte[] requestBuf, int length, IPEndPoint localEndPoint, bool useSocks)
        {
            //try
            //{
            //    string requestString = Encoding.UTF8.GetString(requestBuf);
            //    if (requestString.IndexOf("AppleWebKit") >= 0)
            //    {
            //        string address = "" + localEndPoint.Address + ":" + config.GetCurrentServer().local_port;
            //        proxy = "SOCKS5 " + address + "; SOCKS " + address + ";";
            //    }
            //}
            //catch (Exception e)
            //{
            //    Logging.LogUsefulException(e);
            //}
            return (useSocks ? "SOCKS5 " : "PROXY ") + localEndPoint.Address + ":" + this._config.localPort + ";";
        }
    }
}
