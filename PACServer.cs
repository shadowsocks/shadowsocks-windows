using Shadowsocks.Net;
using Shadowsocks.Utilities;
using Shadowsocks.Net.Crypto;
using Splat;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;

namespace Shadowsocks.PAC
{
    public class PACServer : StreamService, IEnableLogger
    {
        public const string RESOURCE_NAME = "pac";

        private string PacSecret
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedPacSecret))
                {
                    _cachedPacSecret = Base64Url.Encode(RNG.GetBytes(32));
                }
                return _cachedPacSecret;
            }
        }
        private string _cachedPacSecret = "";
        private bool _PACServerEnableSecret;
        public string PacUrl { get; private set; } = "";

        private PACDaemon _pacDaemon;

        public PACServer(PACDaemon pacDaemon, bool PACServerEnableSecret)
        {
            _pacDaemon = pacDaemon;
            _PACServerEnableSecret = PACServerEnableSecret;
        }

        public void UpdatePACURL(EndPoint localEndPoint)
        {
            string usedSecret = _PACServerEnableSecret ? $"&secret={PacSecret}" : "";
            string contentHash = GetHash(_pacDaemon.GetPACContent());
            PacUrl = $"http://{localEndPoint}/{RESOURCE_NAME}?hash={contentHash}{usedSecret}";
            this.Log().Debug("Setting PAC URL: {PacUrl}");
        }

        private static string GetHash(string content)
        {

            return Base64Url.Encode(CryptoUtils.MD5(Encoding.ASCII.GetBytes(content)));
        }

        public override bool Handle(CachedNetworkStream stream, object state)
        {
            byte[] fp = new byte[256];
            int len = stream.ReadFirstBlock(fp);
            return Handle(fp, len, stream.Socket, state);
        }

        [Obsolete]
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
                bool secretMatch = !_PACServerEnableSecret;

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
                            if (kv[1].Trim() == (socket.LocalEndPoint as IPEndPoint)?.ToString())
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
                IPEndPoint localEndPoint = socket.LocalEndPoint as IPEndPoint ?? throw new ArgumentException("Invalid socket local endpoint.", nameof(socket));

                string proxy = GetPACAddress(localEndPoint, useSocks);

                string pacContent = $"var __PROXY__ = '{proxy}';\n" + _pacDaemon.GetPACContent();
                string responseHead =
$@"HTTP/1.1 200 OK
Server: ShadowsocksPAC/{Assembly.GetExecutingAssembly().GetName().Version}
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: { Encoding.UTF8.GetBytes(pacContent).Length}
Connection: Close

";
                byte[] response = Encoding.UTF8.GetBytes(responseHead + pacContent);
                socket.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), socket);
            }
            catch (Exception e)
            {
                this.Log().Error(e, "");
                socket.Close();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket? conn = ar.AsyncState as Socket;
            try
            {
                conn?.Shutdown(SocketShutdown.Send);
            }
            catch
            {
            }
        }

        private string GetPACAddress(IPEndPoint localEndPoint, bool useSocks) => $"{(useSocks ? "SOCKS5" : "PROXY")} {localEndPoint};";
    }
}
