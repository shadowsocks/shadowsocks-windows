using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Shadowsocks.Controller
{
    class PACServer
    {
        private static int PORT = 8090;
        private static string PAC_FILE = "pac.txt";

        Socket _listener;
        FileSystemWatcher watcher;

        public event EventHandler PACFileChanged;

        public void Start(Configuration configuration)
        {
            try
            {
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
            _listener.Close();
            _listener = null;
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
                Socket conn = listener.EndAccept(ar);
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                conn.BeginReceive(new byte[1024], 0, 1024, 0,
                    new AsyncCallback(ReceiveCallback), conn);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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

        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            try
            {
                int bytesRead = conn.EndReceive(ar);

                string pac = GetPACContent();

                IPEndPoint localEndPoint = (IPEndPoint)conn.LocalEndPoint;

                string proxy = "PROXY " + localEndPoint.Address + ":8123;";

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
            conn.Shutdown(SocketShutdown.Send);
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
    }
}
