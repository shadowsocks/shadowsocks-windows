using Shadowsocks.Net;
using Shadowsocks.Net.Crypto;
using Shadowsocks.Utilities;
using Splat;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Shadowsocks.PAC;

public class PacServer(PacDaemon pacDaemon, bool pacServerEnableSecret) : StreamService, IEnableLogger
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
    public string PacUrl { get; private set; } = "";

    public void UpdatePACURL(EndPoint localEndPoint)
    {
        var usedSecret = pacServerEnableSecret ? $"&secret={PacSecret}" : "";
        var contentHash = GetHash(pacDaemon.GetPacContent());
        PacUrl = $"http://{localEndPoint}/{RESOURCE_NAME}?hash={contentHash}{usedSecret}";
        this.Log().Debug("Setting PAC URL: {PacUrl}");
    }

    private static string GetHash(string content)
    => Base64Url.Encode(CryptoUtils.MD5(Encoding.ASCII.GetBytes(content)));

    public override bool Handle(CachedNetworkStream stream, object state)
    {
        var fp = new byte[256];
        var len = stream.ReadFirstBlock(fp);
        return Handle(fp, len, stream.Socket, state);
    }

    [Obsolete]
    public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
    {
        if (socket.ProtocolType != ProtocolType.Tcp) { return false; }

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

            var request = Encoding.UTF8.GetString(firstPacket, 0, length);
            var lines = request.Split('\r', '\n');
            bool hostMatch = false, pathMatch = false, useSocks = false;
            var secretMatch = !pacServerEnableSecret;

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
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                var kv = lines[i].Split(new char[] { ':' }, 2);
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
            var localEndPoint = socket.LocalEndPoint as IPEndPoint ?? throw new ArgumentException("Invalid socket local endpoint.", nameof(socket));

            var proxy = GetPACAddress(localEndPoint, useSocks);

            var pacContent = $"var __PROXY__ = '{proxy}';\n" + pacDaemon.GetPacContent();
            var responseHead =
                $@"HTTP/1.1 200 OK
Server: ShadowsocksPAC/{Assembly.GetExecutingAssembly().GetName().Version}
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: {Encoding.UTF8.GetBytes(pacContent).Length}
Connection: Close

";
            var response = Encoding.UTF8.GetBytes(responseHead + pacContent);
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
        var conn = ar.AsyncState as Socket;
        try
        {
            conn?.Shutdown(SocketShutdown.Send);
        }
        catch { /*ignored*/ }
    }

    private string GetPACAddress(IPEndPoint localEndPoint, bool useSocks) => $"{(useSocks ? "SOCKS5" : "PROXY")} {localEndPoint};";
}