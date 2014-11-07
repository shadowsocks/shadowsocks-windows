using shadowsocks_csharp.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace shadowsocks_csharp.Controller
{
    class PACServer
    {
        private static string PAC_FILE = "pac.txt";

        Socket listener;
        FileSystemWatcher watcher;

        public event EventHandler PACFileChanged;

        public void Start()
        {
            // Create a TCP/IP socket.
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(0, 8090);

            // Bind the socket to the local endpoint and listen for incoming connections.
            listener.Bind(localEndPoint);
            listener.Listen(100);

            listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                listener);
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


        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);
                Socket conn = listener.EndAccept(ar);

                conn.BeginReceive(new byte[1024], 0, 256, 0,
                    new AsyncCallback(receiveCallback), conn);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private string getPACContent()
        {
            // TODO try pac.txt in current directory
            
            if (File.Exists(PAC_FILE))
            {
                watchPACFile();
                return File.ReadAllText(PAC_FILE, Encoding.UTF8);
            }
            else
            {
                byte[] pacGZ = Resources.proxy_pac_txt;
                byte[] buffer = new byte[1024 * 1024];  // builtin pac gzip size: maximum 1M
                int n;

                using (GZipStream input = new GZipStream(new MemoryStream(pacGZ),
                    CompressionMode.Decompress, false))
                {
                    n = input.Read(buffer, 0, buffer.Length);
                    if (n == 0)
                    {
                        throw new IOException("can not decompress pac");
                    }
                    return System.Text.Encoding.UTF8.GetString(buffer, 0, n);
                }
            }

        }

        private void receiveCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            try
            {
                int bytesRead = conn.EndReceive(ar);

                string pac = getPACContent();

                string proxy = "PROXY 127.0.0.1:8123; DIRECT;";

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
                    conn.BeginSend(response, 0, response.Length, 0, new AsyncCallback(sendCallback), conn);
                }
                else
                {
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                conn.Close();
            }
        }

        private void sendCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            conn.Shutdown(SocketShutdown.Send);
        }

        private void watchPACFile()
        {
            if (watcher != null)
            {
                watcher.Dispose();
            }
            watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = PAC_FILE;
            watcher.Changed += watcher_Changed;
            watcher.Created += watcher_Changed;
            watcher.Deleted += watcher_Changed;
            watcher.Renamed += watcher_Changed;
            watcher.EnableRaisingEvents = true;
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (PACFileChanged != null)
            {
                PACFileChanged(this, new EventArgs());
            }
        }
    }
}
