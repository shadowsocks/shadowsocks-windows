using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Shadowsocks.Util
{
    public static class SocketUtil
    {
        private class DnsEndPoint2 : DnsEndPoint
        {
            public DnsEndPoint2(string host, int port) : base(host, port)
            {
            }

            public DnsEndPoint2(string host, int port, AddressFamily addressFamily) : base(host, port, addressFamily)
            {
            }

            public override string ToString()
            {
                return this.Host + ":" + this.Port;
            }
        }

        public static EndPoint GetEndPoint(string host, int port)
        {
            IPAddress ipAddress;
            bool parsed = IPAddress.TryParse(host, out ipAddress);
            if (parsed)
            {
                return new IPEndPoint(ipAddress, port);
            }

            // maybe is a domain name
            return new DnsEndPoint2(host, port);
        }

        private class TcpUserToken : IAsyncResult
        {
            public AsyncCallback Callback { get; }
            public SocketAsyncEventArgs Args { get; }

            public TcpUserToken(AsyncCallback callback, object state, SocketAsyncEventArgs args)
            {
                Callback = callback;
                AsyncState = state;
                Args = args;
            }

            public bool IsCompleted { get; } = true;
            public WaitHandle AsyncWaitHandle { get; } = null;
            public object AsyncState { get; }
            public bool CompletedSynchronously { get; } = true;
        }

        private static void OnTcpConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            TcpUserToken token = (TcpUserToken) args.UserToken;

            token.Callback(token);
        }

        public static void BeginConnectTcp(EndPoint endPoint, AsyncCallback callback, object state)
        {
            var arg = new SocketAsyncEventArgs();
            arg.RemoteEndPoint = endPoint;
            arg.Completed += OnTcpConnectCompleted;
            arg.UserToken = new TcpUserToken(callback, state, arg);


            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, arg);
        }

        public static Socket EndConnectTcp(IAsyncResult asyncResult)
        {
            var tut = asyncResult as TcpUserToken;
            if (tut == null)
            {
                throw new ArgumentException("Invalid asyncResult.", nameof(asyncResult));
            }

            var arg = tut.Args;

            if (arg.SocketError != SocketError.Success)
            {
                if (arg.ConnectByNameError != null)
                {
                    throw arg.ConnectByNameError;
                }

                var ex = new SocketException((int)arg.SocketError);
                throw ex;
            }

            var so = tut.Args.ConnectSocket;

            so.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            return so;
        }
    }
}
