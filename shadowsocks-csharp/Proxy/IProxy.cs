using System;
using System.Net;
using System.Net.Sockets;

namespace Shadowsocks.Proxy
{

    public interface IProxy
    {
        EndPoint LocalEndPoint { get; }

        EndPoint ProxyEndPoint { get; }

        EndPoint DestEndPoint { get; }

        IAsyncResult BeginConnectProxy(EndPoint remoteEP, AsyncCallback callback, object state);

        void EndConnectProxy(IAsyncResult asyncResult);

        IAsyncResult BeginConnectDest(EndPoint remoteEP, AsyncCallback callback, object state);

        void EndConnectDest(IAsyncResult asyncResult);

        IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state);

        int EndSend(IAsyncResult asyncResult);

        IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state);

        int EndReceive(IAsyncResult asyncResult);

        void Shutdown(SocketShutdown how);

        void Close();
    }
}
