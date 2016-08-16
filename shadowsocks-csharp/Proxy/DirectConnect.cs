using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Shadowsocks.Proxy
{
    public class DirectConnect : IProxy
    {
        private class FakeAsyncResult : IAsyncResult
        {
            public FakeAsyncResult(object state)
            {
                AsyncState = state;
            }

            public bool IsCompleted { get; } = true;
            public WaitHandle AsyncWaitHandle { get; } = null;
            public object AsyncState { get; }
            public bool CompletedSynchronously { get; } = true;
        }

        private class FakeEndPoint : EndPoint
        {
            public override AddressFamily AddressFamily { get; } = AddressFamily.Unspecified;

            public override string ToString()
            {
                return "null proxy";
            }
        }

        private Socket _remote;

        public EndPoint LocalEndPoint => _remote.LocalEndPoint;

        public EndPoint ProxyEndPoint { get; } = new FakeEndPoint();
        public EndPoint DestEndPoint { get; private set; }

        public void BeginConnectProxy(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            // do nothing

            var r = new FakeAsyncResult(state);
            callback?.Invoke(r);
        }

        public void EndConnectProxy(IAsyncResult asyncResult)
        {
            // do nothing
        }

        public void BeginConnectDest(EndPoint destEndPoint, AsyncCallback callback, object state)
        {
            EndPoint realEndPoint = DestEndPoint = destEndPoint;

            /*
             * On windows vista or later, dual-mode socket is supported, so that
             * we don't need to resolve a DnsEndPoint manually.
             * We could just create a dual-mode socket and pass the DnsEndPoint
             * directly to it's BeginConnect and the system will handle it correctlly
             * so that we won't worry about async resolving any more.
             * 
             * see: https://blogs.msdn.microsoft.com/webdev/2013/01/08/dual-mode-sockets-never-create-an-ipv4-socket-again/
             * 
             * But it seems that we can't use this feature because DnsEndPoint
             * doesn't have a specific AddressFamily before it has been
             * resolved (we don't know whether it's ipv4 or ipv6) and we don't have
             * a dual-mode socket to use on windows xp :(
             */
            var dep = realEndPoint as DnsEndPoint;
            if (dep != null)
            {
                // need to resolve manually
                // TODO async resolving
                IPHostEntry ipHostInfo = Dns.GetHostEntry(dep.Host);
                IPAddress ipAddress = ipHostInfo.AddressList[0];

                realEndPoint = new IPEndPoint(ipAddress, dep.Port);
            }

            if (_remote == null)
            {
                _remote = new Socket(realEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }
            _remote.BeginConnect(realEndPoint, callback, state);
        }

        public void EndConnectDest(IAsyncResult asyncResult)
        {
            _remote?.EndConnect(asyncResult);
        }

        public void BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state)
        {
            _remote?.BeginSend(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            return _remote.EndSend(asyncResult);
        }

        public void BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state)
        {
            _remote?.BeginReceive(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            return _remote.EndReceive(asyncResult);
        }

        public void Shutdown(SocketShutdown how)
        {
            _remote?.Shutdown(how);
        }

        public void Close()
        {
            _remote?.Close();
        }
    }
}
