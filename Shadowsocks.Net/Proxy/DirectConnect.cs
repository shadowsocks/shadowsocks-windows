using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Net.Proxy;

public class DirectConnect : IProxy
{
    private class FakeAsyncResult(object state) : IAsyncResult
    {
        public bool IsCompleted { get; } = true;
        public WaitHandle AsyncWaitHandle { get; } = null;
        public object AsyncState { get; } = state;
        public bool CompletedSynchronously { get; } = true;
    }

    private class FakeEndPoint : EndPoint
    {
        public override AddressFamily AddressFamily { get; } = AddressFamily.Unspecified;

        public override string ToString() => "null proxy";
    }

    private readonly Socket _remote = new(SocketType.Stream, ProtocolType.Tcp);

    public EndPoint LocalEndPoint => _remote.LocalEndPoint;

    public EndPoint ProxyEndPoint { get; } = new FakeEndPoint();
    public EndPoint DestEndPoint { get; private set; }

    public void Shutdown(SocketShutdown how) => _remote.Shutdown(how);

    public void Close() => _remote.Dispose();

    public Task ConnectProxyAsync(EndPoint remoteEP, NetworkCredential auth = null, CancellationToken token = default) => Task.CompletedTask;

    public async Task ConnectRemoteAsync(EndPoint destEndPoint, CancellationToken token = default)
    {
        DestEndPoint = destEndPoint;
        await _remote.ConnectAsync(destEndPoint, token);
    }

    public async Task<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    => await _remote.SendAsync(buffer, SocketFlags.None, token);

    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token = default)
    => await _remote.ReceiveAsync(buffer, SocketFlags.None, token);
}