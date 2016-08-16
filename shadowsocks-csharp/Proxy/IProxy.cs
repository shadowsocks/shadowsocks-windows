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

        void BeginConnectProxy(EndPoint remoteEP, AsyncCallback callback, object state);

        void EndConnectProxy(IAsyncResult asyncResult);

        void BeginConnectDest(EndPoint destEndPoint, AsyncCallback callback, object state);

        void EndConnectDest(IAsyncResult asyncResult);

        void BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state);

        int EndSend(IAsyncResult asyncResult);

        void BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state);

        int EndReceive(IAsyncResult asyncResult);

        void Shutdown(SocketShutdown how);

        void Close();
    }

    public static class ProxyUtils
    {
        public static EndPoint GetEndPoint(string host, int port)
        {
            IPAddress ipAddress;
            bool parsed = IPAddress.TryParse(host, out ipAddress);
            if (parsed)
            {
                return new IPEndPoint(ipAddress, port);
            }

            // maybe is a domain name
            return new DnsEndPoint(host, port);
        }
    }
}
