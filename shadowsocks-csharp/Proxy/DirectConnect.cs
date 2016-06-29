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

        private Socket _remote;

        public EndPoint LocalEndPoint => _remote.LocalEndPoint;

        public EndPoint ProxyEndPoint { get; private set; }

        public EndPoint DestEndPoint { get; private set; }


        public void BeginConnectProxy(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            // do nothing
            ProxyEndPoint = remoteEP;

            var r = new FakeAsyncResult(state);
            callback?.Invoke(r);
        }

        public void EndConnectProxy(IAsyncResult asyncResult)
        {
            // do nothing
        }

        public void BeginConnectDest(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            if (_remote == null)
            {
                _remote = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }

            DestEndPoint = remoteEP;

            _remote.BeginConnect(remoteEP, callback, state);
        }

        public void EndConnectDest(IAsyncResult asyncResult)
        {
            _remote.EndConnect(asyncResult);
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
            _remote?.Shutdown(how);
        }

        public void Close()
        {
            _remote?.Close();
        }
    }
}
