using Splat;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Net.Proxy
{
    public class HttpProxy : IProxy, IEnableLogger
    {
        public EndPoint LocalEndPoint => _remote.LocalEndPoint;
        public EndPoint ProxyEndPoint { get; private set; }
        public EndPoint DestEndPoint { get; private set; }

        private readonly Socket _remote = new Socket(SocketType.Stream, ProtocolType.Tcp);

        private const string HTTP_CRLF = "\r\n";
        private const string HTTP_CONNECT_TEMPLATE =
            "CONNECT {0} HTTP/1.1" + HTTP_CRLF +
            "Host: {0}" + HTTP_CRLF +
            "Proxy-Connection: keep-alive" + HTTP_CRLF +
            "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36" + HTTP_CRLF +
            "{1}" +         // Proxy-Authorization if any
            "" + HTTP_CRLF; // End with an empty line
        private const string PROXY_AUTH_TEMPLATE = "Proxy-Authorization: Basic {0}" + HTTP_CRLF;

        public void Shutdown(SocketShutdown how)
        {
            _remote.Shutdown(how);
        }

        public void Close()
        {
            _remote.Dispose();
        }

        private static readonly Regex HttpRespondHeaderRegex = new Regex(@"^(HTTP/1\.\d) (\d{3}) (.+)$", RegexOptions.Compiled);
        private int _respondLineCount = 0;
        private bool _established = false;

        private bool OnLineRead(string line, object state)
        {
            this.Log().Debug(line);

            if (_respondLineCount == 0)
            {
                var m = HttpRespondHeaderRegex.Match(line);
                if (m.Success)
                {
                    var resultCode = m.Groups[2].Value;
                    if ("200" != resultCode)
                    {
                        return true;
                    }
                    _established = true;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(line))
                {
                    return true;
                }
            }
            _respondLineCount++;

            return false;
        }

        private NetworkCredential auth;

        public async Task ConnectProxyAsync(EndPoint remoteEP, NetworkCredential auth = null, CancellationToken token = default)
        {
            ProxyEndPoint = remoteEP;
            this.auth = auth;
            await _remote.ConnectAsync(remoteEP);
            _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        }

        public async Task ConnectRemoteAsync(EndPoint destEndPoint, CancellationToken token = default)
        {
            DestEndPoint = destEndPoint;
            String authInfo = "";
            if (auth != null)
            {
                string authKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(auth.UserName + ":" + auth.Password));
                authInfo = string.Format(PROXY_AUTH_TEMPLATE, authKey);
            }
            string request = string.Format(HTTP_CONNECT_TEMPLATE, destEndPoint, authInfo);

            var b = Encoding.UTF8.GetBytes(request);

            await _remote.SendAsync(Encoding.UTF8.GetBytes(request), SocketFlags.None, token);

            // start line read
            LineReader reader = new LineReader(_remote, OnLineRead, (e, _) => throw e, (_1, _2, _3, _4) => { }, Encoding.UTF8, HTTP_CRLF, 1024, null);
            await reader.Finished;
        }

        public async Task<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            return await _remote.SendAsync(buffer, SocketFlags.None, token);
        }

        public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            return await _remote.ReceiveAsync(buffer, SocketFlags.None, token);
        }
    }
}
