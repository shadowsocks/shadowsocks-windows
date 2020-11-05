using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Net.Proxy
{
    public interface IProxy
    {
        EndPoint LocalEndPoint { get; }

        EndPoint ProxyEndPoint { get; }

        EndPoint DestEndPoint { get; }

        Task ConnectProxyAsync(EndPoint remoteEP, NetworkCredential auth = null, CancellationToken token = default);

        Task ConnectRemoteAsync(EndPoint destEndPoint, CancellationToken token = default);

        Task<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default);

        Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token = default);

        void Shutdown(SocketShutdown how);

        void Close();
    }
}
