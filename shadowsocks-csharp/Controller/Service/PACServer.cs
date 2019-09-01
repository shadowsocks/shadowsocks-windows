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
        public const string RESOURCE_NAME = "pac";

        private string PacSecret { get; set; } = "";

        public string PacUrl { get; private set; } = "";

        private Configuration _config;
        private PACDaemon _pacDaemon;

        public PACServer(PACDaemon pacDaemon)
        {
            _pacDaemon = pacDaemon;
        }

        public void UpdatePACURL(Configuration config)
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

            PacUrl = $"http://{config.localHost}:{config.localPort}/{RESOURCE_NAME}?t={GetTimestamp(DateTime.Now)}{PacSecret}";
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
                /*
                 *  RFC 7230
                 *  
                    GET /hello.txt HTTP/1.1
                    User-Agent: curl/7.16.3 libcurl/7.16.3 OpenSSL/0.9.7l zlib/1.2.3
                    Host: www.example.com
                    Accept-Language: en, mi 
                 */
        
                string request = Encoding.UTF8.GetString(firstPacket, 0, length);
                string[] lines = request.Split('\r', '\n');
                bool hostMatch = false, pathMatch = false, useSocks = false;
                bool secretMatch = PacSecret.IsNullOrEmpty();

                if (lines.Length < 2)   // need at lease RequestLine + Host
                {
                    return false;
                }

                // parse request line
                string requestLine = lines[0];
                // GET /pac?t=yyyyMMddHHmmssfff&secret=foobar HTTP/1.1
                string[] requestItems = requestLine.Split(' ');
                if (requestItems.Length == 3 && requestItems[0] == "GET")
                {
                    int index = requestItems[1].IndexOf('?');
                    if (index < 0)
                    {
                        index = requestItems[1].Length;
                    }
                    string resourceString = requestItems[1].Substring(0, index).Remove(0, 1);
                    if (string.Equals(resourceString, RESOURCE_NAME, StringComparison.OrdinalIgnoreCase))
                    {
                        pathMatch = true;
                        if (!secretMatch)
                        {
                            string queryString = requestItems[1].Substring(index);
                            if (queryString.Contains(PacSecret))
                            {
                                secretMatch = true;
                            }
                        }
                    }
                }

                // parse request header
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrEmpty(lines[i]))
                        continue;

                    string[] kv = lines[i].Split(new char[] { ':' }, 2);
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



        public void SendResponse(Socket socket, bool useSocks)
        {
            try
            {
                IPEndPoint localEndPoint = (IPEndPoint)socket.LocalEndPoint;

                string proxy = GetPACAddress(localEndPoint, useSocks);

                string pacContent = _pacDaemon.GetPACContent().Replace("__PROXY__", proxy);

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


        private string GetPACAddress(IPEndPoint localEndPoint, bool useSocks)
        {
            return localEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                ? $"{(useSocks ? "SOCKS5" : "PROXY")} [{localEndPoint.Address}]:{_config.localPort};"
                : $"{(useSocks ? "SOCKS5" : "PROXY")} {localEndPoint.Address}:{_config.localPort};";
        }
    }
}
