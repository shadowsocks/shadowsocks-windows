using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Shadowsocks.Controller;
using Shadowsocks.Util.Sockets;

namespace Shadowsocks.Proxy
{
    public class HttpProxy : IProxy
    {
        private class FakeAsyncResult : IAsyncResult
        {
            public readonly HttpState innerState;

            private readonly IAsyncResult r;

            public FakeAsyncResult(IAsyncResult orig, HttpState state)
            {
                r = orig;
                innerState = state;
            }

            public bool IsCompleted => r.IsCompleted;
            public WaitHandle AsyncWaitHandle => r.AsyncWaitHandle;
            public object AsyncState => innerState.AsyncState;
            public bool CompletedSynchronously => r.CompletedSynchronously;
        }

        private class HttpState
        {

            public AsyncCallback Callback { get; set; }

            public object AsyncState { get; set; }

            public int BytesToRead;

            public Exception ex { get; set; }
        }

        public EndPoint LocalEndPoint => _remote.LocalEndPoint;
        public EndPoint ProxyEndPoint { get; private set; }
        public EndPoint DestEndPoint { get; private set; }


        private readonly WrappedSocket _remote = new WrappedSocket();


        public void BeginConnectProxy(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            ProxyEndPoint = remoteEP;

            _remote.BeginConnect(remoteEP, callback, state);
        }

        public void EndConnectProxy(IAsyncResult asyncResult)
        {
            _remote.EndConnect(asyncResult);
            _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        }

        private const string HTTP_CRLF = "\r\n";
        private const string HTTP_CONNECT_TEMPLATE = 
            "CONNECT {0} HTTP/1.1" + HTTP_CRLF + 
            "Host: {0}" + HTTP_CRLF +
            "Proxy-Connection: keep-alive" + HTTP_CRLF +
            "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36" + HTTP_CRLF +
            "" + HTTP_CRLF; // End with an empty line

        public void BeginConnectDest(EndPoint destEndPoint, AsyncCallback callback, object state)
        {
            DestEndPoint = destEndPoint;
            string request = string.Format(HTTP_CONNECT_TEMPLATE, destEndPoint);

            var b = Encoding.UTF8.GetBytes(request);

            var st = new HttpState();
            st.Callback = callback;
            st.AsyncState = state;

            _remote.BeginSend(b, 0, b.Length, 0, HttpRequestSendCallback, st);
        }

        public void EndConnectDest(IAsyncResult asyncResult)
        {
            var state = ((FakeAsyncResult)asyncResult).innerState;

            if (state.ex != null)
            {
                throw state.ex;
            }
        }

        public void BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state)
        {
            _remote.BeginSend(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            return _remote.EndSend(asyncResult);
        }

        public void BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state)
        {
            _remote.BeginReceive(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            return _remote.EndReceive(asyncResult);
        }

        public void Shutdown(SocketShutdown how)
        {
            _remote.Shutdown(how);
        }

        public void Close()
        {
            _remote.Dispose();
        }

        private void HttpRequestSendCallback(IAsyncResult ar)
        {
            var state = (HttpState) ar.AsyncState;
            try
            {
                _remote.EndSend(ar);

                // start line read
                new LineReader(_remote, OnLineRead, OnException, OnFinish, Encoding.UTF8, HTTP_CRLF, 1024, new FakeAsyncResult(ar, state));
            }
            catch (Exception ex)
            {
                state.ex = ex;
                state.Callback?.Invoke(new FakeAsyncResult(ar, state));
            }
        }

        private void OnFinish(byte[] lastBytes, int index, int length, object state)
        {
            var st = (FakeAsyncResult)state;

            if (st.innerState.ex == null)
            {
                if (!_established)
                {
                    st.innerState.ex = new Exception(I18N.GetString("Proxy request failed"));
                }
                // TODO: save last bytes
            }
            st.innerState.Callback?.Invoke(st);
        }

        private void OnException(Exception ex, object state)
        {
            var st = (FakeAsyncResult) state;

            st.innerState.ex = ex;
        }

        private static readonly Regex HttpRespondHeaderRegex = new Regex(@"^(HTTP/1\.\d) (\d{3}) (.+)$");
        private int _respondLineCount = 0;
        private bool _established = false;

        private bool OnLineRead(string line, object state)
        {
            Logging.Debug(line);

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
                if (line.IsNullOrEmpty())
                {
                    return true;
                }
            }
            _respondLineCount++;

            return false;
        }
    }
}
