using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shadowsocks.Core.Properties;
using Shadowsocks.Models;

namespace Shadowsocks.Services
{
    class PACServer : Listener.Service
    {
        public static readonly string PAC_FILE = "pac.txt";
        public static readonly string USER_RULE_FILE = "user-rule.txt";
        public static readonly string USER_ABP_FILE = "abp.txt";

        FileSystemWatcher PACFileWatcher;
        FileSystemWatcher UserRuleFileWatcher;
        private IConfig _config;

        public event EventHandler PACFileChanged;
        public event EventHandler UserRuleFileChanged;

        public PACServer()
        {
            this.WatchPacFile();
            this.WatchUserRuleFile();
        }

        public void UpdateConfiguration(IConfig config)
        {
            this._config = config;
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
                        if (line.IndexOf("pac", StringComparison.Ordinal) >= 0)
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
            if (File.Exists(PAC_FILE))
            {
                return PAC_FILE;
            }
            Utils.UncompressFile(PAC_FILE, Resources.proxy_pac_txt);
            return PAC_FILE;
        }

        internal string TouchUserRuleFile()
        {
            if (File.Exists(USER_RULE_FILE))
            {
                return USER_RULE_FILE;
            }
            File.WriteAllText(USER_RULE_FILE, Resources.user_rule);
            return USER_RULE_FILE;
        }

        private string GetPACContent()
        {
            return File.Exists(PAC_FILE) ? File.ReadAllText(PAC_FILE, Encoding.UTF8) : Utils.UnGzip(Resources.proxy_pac_txt);
        }

        public void SendResponse(byte[] firstPacket, int length, Socket socket, bool useSocks)
        {
            try
            {
                string pac = GetPACContent();

                IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;

                string proxy = GetPACAddress(firstPacket, length, localEndPoint, useSocks);

                pac = pac.Replace("__PROXY__", proxy);

                string text = $@"HTTP/1.1 200 OK
Server: Shadowsocks
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: {Encoding.UTF8.GetBytes(pac).Length}
Connection: Close

" + pac;
                byte[] response = Encoding.UTF8.GetBytes(text);
                socket.BeginSend(response, 0, response.Length, 0, SendCallback, socket);
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
            PACFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory())
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = PAC_FILE
            };
            PACFileWatcher.Changed += PACFileWatcher_Changed;
            PACFileWatcher.Created += PACFileWatcher_Changed;
            PACFileWatcher.Deleted += PACFileWatcher_Changed;
            PACFileWatcher.Renamed += PACFileWatcher_Changed;
            PACFileWatcher.EnableRaisingEvents = true;
        }

        private void WatchUserRuleFile()
        {
            UserRuleFileWatcher?.Dispose();
            UserRuleFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory())
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = USER_RULE_FILE
            };
            UserRuleFileWatcher.Changed += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Created += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Deleted += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Renamed += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.EnableRaisingEvents = true;
        }

        #region FileSystemWatcher.OnChanged()
        // FileSystemWatcher Changed event is raised twice
        // http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
        private static Hashtable fileChangedTime = new Hashtable();

        private void PACFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();

            // if there is no path info stored yet or stored path has different time of write then the one now is inspected
            if (!fileChangedTime.ContainsKey(path) || fileChangedTime[path].ToString() != currentLastWriteTime)
            {
                if (PACFileChanged != null)
                {
                    Logging.Info($"Detected: PAC file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
                    PACFileChanged(this, new EventArgs());
                }

                // lastly we update the last write time in the hashtable
                fileChangedTime[path] = currentLastWriteTime;
            }
        }

        private void UserRuleFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();

            // if there is no path info stored yet or stored path has different time of write then the one now is inspected
            if (!fileChangedTime.ContainsKey(path) || fileChangedTime[path].ToString() != currentLastWriteTime)
            {
                if (UserRuleFileChanged != null)
                {
                    Logging.Info($"Detected: User Rule file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
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

        public bool AddLine(string rule)
        {
            var hehe = File.Exists(USER_RULE_FILE) ? File.ReadAllText(USER_RULE_FILE, Encoding.UTF8) : Resources.user_rule;
            if (hehe.Contains(rule))
            {
                File.WriteAllText(USER_RULE_FILE, hehe.Replace($"\r\n{rule}", ""));
                return false;
            }
            hehe += $"\r\n{rule}";
            File.WriteAllText(USER_RULE_FILE, hehe);
            return true;
        }
    }
}
