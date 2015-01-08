using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Shadowsocks.Controller
{
    class PACServer
    {
        private static int PORT = 8093;
        private static string PAC_FILE = "pac.txt";
        private static Configuration config;

        Socket _listener;
        FileSystemWatcher watcher;

        public event EventHandler PACFileChanged;

        public event EventHandler UpdatePACFromGFWListCompleted;

        public event ErrorEventHandler UpdatePACFromGFWListError;

        public void Start(Configuration configuration)
        {
            try
            {
                config = configuration;
                // Create a TCP/IP socket.
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                if (configuration.shareOverLan)
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, PORT);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Loopback, PORT);
                }

                // Bind the socket to the local endpoint and listen for incoming connections.
                _listener.Bind(localEndPoint);
                _listener.Listen(100);
                _listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    _listener);

                WatchPacFile();
            }
            catch (SocketException)
            {
                _listener.Close();
                throw;
            }
        }

        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Close();
                _listener = null;
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

        // we don't even use it
        static byte[] requestBuf = new byte[2048];

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                Socket conn = listener.EndAccept(ar);

                object[] state = new object[] {
                    conn,
                    requestBuf
                };

                conn.BeginReceive(requestBuf, 0, requestBuf.Length, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                try
                {
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                }
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
                byte[] pacGZ = Resources.proxy_pac_txt;
                byte[] buffer = new byte[1024];  // builtin pac gzip size: maximum 100K
                MemoryStream sb = new MemoryStream();
                int n;
                using (GZipStream input = new GZipStream(new MemoryStream(pacGZ),
                    CompressionMode.Decompress, false))
                {
                    while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Write(buffer, 0, n);
                    }
                    return System.Text.Encoding.UTF8.GetString(sb.ToArray());
                }
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;

            Socket conn = (Socket)state[0];
            byte[] requestBuf = (byte[])state[1];
            try
            {
                int bytesRead = conn.EndReceive(ar);

                string pac = GetPACContent();

                IPEndPoint localEndPoint = (IPEndPoint)conn.LocalEndPoint;

                string proxy = GetPACAddress(requestBuf, localEndPoint);

                pac = pac.Replace("__PROXY__", proxy);

                if (bytesRead > 0)
                {
                    string text = String.Format(@"HTTP/1.1 200 OK
Server: Shadowsocks
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: {0}
Connection: Close

", System.Text.Encoding.UTF8.GetBytes(pac).Length) + pac;
                    byte[] response = System.Text.Encoding.UTF8.GetBytes(text);
                    conn.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), conn);
                    Util.Util.ReleaseMemory();
                }
                else
                {
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                conn.Close();
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
            if (watcher != null)
            {
                watcher.Dispose();
            }
            watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = PAC_FILE;
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Deleted += Watcher_Changed;
            watcher.Renamed += Watcher_Changed;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (PACFileChanged != null)
            {
                PACFileChanged(this, new EventArgs());
            }
        }

        private string GetPACAddress(byte[] requestBuf, IPEndPoint localEndPoint)
        {
            string proxy = "PROXY " + localEndPoint.Address + ":8123;";
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
            //    Console.WriteLine(e);
            //}
            return proxy;
        }

        public void UpdatePACFromGFWList()
        {
            GfwListUpdater gfwlist = new GfwListUpdater();
            gfwlist.DownloadCompleted += gfwlist_DownloadCompleted;
            gfwlist.Error += gfwlist_Error;
            gfwlist.proxy = new WebProxy(IPAddress.Loopback.ToString(), 8123); /* use polipo proxy*/
            gfwlist.Download();
        }

        private void gfwlist_DownloadCompleted(object sender, GfwListUpdater.GfwListDownloadCompletedArgs e)
        {
            GfwListUpdater.Parser parser = new GfwListUpdater.Parser(e.Content);
            string[] lines = parser.GetValidLines();
            StringBuilder rules = new StringBuilder(lines.Length * 16);
            SerializeRules(lines, rules);
            string abpContent = GetAbpContent();
            abpContent = abpContent.Replace("__RULES__", rules.ToString());
            File.WriteAllText(PAC_FILE, abpContent);
            if (UpdatePACFromGFWListCompleted != null)
            {
                UpdatePACFromGFWListCompleted(this, new EventArgs());
            }
        }

        private void gfwlist_Error(object sender, ErrorEventArgs e)
        {
            if (UpdatePACFromGFWListError != null)
            {
                UpdatePACFromGFWListError(this, e);
            }
        }

        private string GetAbpContent()
        {
            byte[] abpGZ = Resources.abp_js;
            byte[] buffer = new byte[1024];  // builtin pac gzip size: maximum 100K
            int n;
            using (MemoryStream sb = new MemoryStream())
            {
                using (GZipStream input = new GZipStream(new MemoryStream(abpGZ),
                    CompressionMode.Decompress, false))
                {
                    while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Write(buffer, 0, n);
                    }
                }
                return System.Text.Encoding.UTF8.GetString(sb.ToArray());
            }
        }

        private static void SerializeRules(string[] rules, StringBuilder builder)
        {
            builder.Append("[\n");

            bool first = true;
            foreach (string rule in rules)
            {
                if (!first)
                    builder.Append(",\n");

                SerializeString(rule, builder);

                first = false;
            }

            builder.Append("\n]");
        }

        private static void SerializeString(string aString, StringBuilder builder)
        {
            builder.Append("\t\"");

            char[] charArray = aString.ToCharArray();
            for (int i = 0; i < charArray.Length; i++)
            {
                char c = charArray[i];
                if (c == '"')
                    builder.Append("\\\"");
                else if (c == '\\')
                    builder.Append("\\\\");
                else if (c == '\b')
                    builder.Append("\\b");
                else if (c == '\f')
                    builder.Append("\\f");
                else if (c == '\n')
                    builder.Append("\\n");
                else if (c == '\r')
                    builder.Append("\\r");
                else if (c == '\t')
                    builder.Append("\\t");
                else
                    builder.Append(c);
            }

            builder.Append("\"");
        }

    }
}
