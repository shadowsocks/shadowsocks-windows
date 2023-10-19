using Shadowsocks.Models;
using Shadowsocks.Net.Crypto;
using Splat;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Shadowsocks.Net;

public class UdpRelay(Server server) : DatagramService
{
    // TODO: choose a smart number
    private readonly LruCache<IPEndPoint, UdpHandler> _cache = new(512);

    public long outbound = 0;
    public long inbound = 0;

    public override async Task<bool> Handle(Memory<byte> packet, Socket socket, EndPoint client)
    {
        if (socket.ProtocolType != ProtocolType.Udp || packet.Length < 4) { return false; }

        var remoteEndPoint = (IPEndPoint)client;
        var handler = _cache.get(remoteEndPoint);
        if (handler == null)
        {
            handler = new UdpHandler(socket, server, remoteEndPoint);
            handler.Receive();
            _cache.Add(remoteEndPoint, handler);
        }
        await handler.SendAsync(packet);
        return true;
    }

    public class UdpHandler : IEnableLogger
    {
        private static readonly MemoryPool<byte> _pool = MemoryPool<byte>.Shared;
        private readonly Socket _local;
        private readonly Socket _remote;

        private readonly Server _server;
        private readonly byte[] _buffer = new byte[65536];

        private readonly IPEndPoint _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;

        private IPAddress ListenAddress
        => _remote.AddressFamily switch
        {
            AddressFamily.InterNetwork => IPAddress.Any,
            AddressFamily.InterNetworkV6 => IPAddress.IPv6Any,
            _ => throw new NotSupportedException(),
        };

        public UdpHandler(Socket local, Server server, IPEndPoint localEndPoint)
        {
            _local = local;
            _server = server;
            _localEndPoint = localEndPoint;

            // TODO async resolving
            var parsed = IPAddress.TryParse(server.Host, out var ipAddress);
            if (!parsed)
            {
                var ipHostInfo = Dns.GetHostEntry(server.Host);
                ipAddress = ipHostInfo.AddressList[0];
            }
            _remoteEndPoint = new IPEndPoint(ipAddress, server.Port);
            _remote = new Socket(_remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _remote.Bind(new IPEndPoint(ListenAddress, 0));
        }

        public async Task SendAsync(ReadOnlyMemory<byte> data)
        {
            using var encryptor = CryptoFactory.GetEncryptor(_server.Method, _server.Password);
            using var mem = _pool.Rent(data.Length + 1000);

            // byte[] dataOut = new byte[slicedData.Length + 1000];
            int outlen = encryptor.EncryptUDP(data.Span[3..], mem.Memory.Span);
            this.Log().Debug($"{_localEndPoint} {_remoteEndPoint} {outlen} UDP Relay up");
            if (!MemoryMarshal.TryGetArray(mem.Memory[..outlen], out ArraySegment<byte> outData))
            {
                throw new InvalidOperationException("Can't extract underly array segment");
            };
            await _remote?.SendToAsync(outData, SocketFlags.None, _remoteEndPoint);
        }

        public async Task ReceiveAsync()
        {
            EndPoint remoteEndPoint = new IPEndPoint(ListenAddress, 0);
            this.Log().Debug($"++++++Receive Server Port, size:" + _buffer.Length);
            try
            {
                while (true)
                {
                    var result = await _remote.ReceiveFromAsync(_buffer, SocketFlags.None, remoteEndPoint);
                    var bytesRead = result.ReceivedBytes;

                    using var owner = _pool.Rent(bytesRead + 3);
                    var o = owner.Memory;

                    using ICrypto encryptor = CryptoFactory.GetEncryptor(_server.Method, _server.Password);
                    var outlen = encryptor.DecryptUDP(o.Span[3..], _buffer.AsSpan(0, bytesRead));
                    this.Log().Debug($"{_remoteEndPoint} {_localEndPoint} {outlen} UDP Relay down");
                    if (!MemoryMarshal.TryGetArray(o[..(outlen + 3)], out ArraySegment<byte> data))
                    {
                        throw new InvalidOperationException("Can't extract underly array segment");
                    };
                    await _local?.SendToAsync(data, SocketFlags.None, _localEndPoint);

                }
            }
            catch (Exception e)
            {
                this.Log().Warn(e, "");
            }
        }

        public void Receive()
        {
            _ = ReceiveAsync();
        }

        public void Close()
        {
            try
            {
                _remote?.Close();
            }
            catch (ObjectDisposedException)
            {
                // TODO: handle the ObjectDisposedException
            }
            catch (Exception)
            {
                // TODO: need more think about handle other Exceptions, or should remove this catch().
            }
        }
    }
}

#region LRU cache

// cc by-sa 3.0 http://stackoverflow.com/a/3719378/1124054
internal class LruCache<TK, TV>(int capacity)
    where TV : UdpRelay.UdpHandler
{
    private readonly Dictionary<TK, LinkedListNode<LruCacheItem<TK, TV>>> _cacheMap = [];
    private readonly LinkedList<LruCacheItem<TK, TV>> _lruList = new();

    [MethodImpl(MethodImplOptions.Synchronized)]
    public TV get(TK key)
    {
        LinkedListNode<LruCacheItem<TK, TV>> node;
        if (_cacheMap.TryGetValue(key, out node))
        {
            var value = node.Value.value;
            _lruList.Remove(node);
            _lruList.AddLast(node);
            return value;
        }
        return default(TV);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Add(TK key, TV val)
    {
        if (_cacheMap.Count >= capacity)
        {
            RemoveFirst();
        }

        var cacheItem = new LruCacheItem<TK, TV>(key, val);
        var node = new LinkedListNode<LruCacheItem<TK, TV>>(cacheItem);
        _lruList.AddLast(node);
        _cacheMap.Add(key, node);
    }

    private void RemoveFirst()
    {
        // Remove from LRUPriority
        var node = _lruList.First;
        _lruList.RemoveFirst();

        // Remove from cache
        _cacheMap.Remove(node.Value.key);
        node.Value.value.Close();
    }
}

internal class LruCacheItem<TK, TV>(TK k, TV v)
{
    public TK key = k;
    public TV value = v;
}

#endregion