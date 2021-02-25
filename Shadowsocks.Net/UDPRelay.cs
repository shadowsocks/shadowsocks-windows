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

namespace Shadowsocks.Net
{
    class UDPRelay : DatagramService
    {
        Server _server;
        // TODO: choose a smart number
        private LRUCache<IPEndPoint, UDPHandler> _cache = new LRUCache<IPEndPoint, UDPHandler>(512);

        public long outbound = 0;
        public long inbound = 0;

        public UDPRelay(Server server)
        {
            _server = server;
        }

        public override async Task<bool> Handle(Memory<byte> packet, Socket socket, EndPoint client)
        {
            if (socket.ProtocolType != ProtocolType.Udp)
            {
                return false;
            }
            if (packet.Length < 4)
            {
                return false;
            }
            IPEndPoint remoteEndPoint = (IPEndPoint)client;
            UDPHandler handler = _cache.get(remoteEndPoint);
            if (handler == null)
            {
                handler = new UDPHandler(socket, _server, remoteEndPoint);
                handler.Receive();
                _cache.add(remoteEndPoint, handler);
            }
            await handler.SendAsync(packet);
            return true;
        }

        public class UDPHandler : IEnableLogger
        {
            private static MemoryPool<byte> pool = MemoryPool<byte>.Shared;
            private Socket _local;
            private Socket _remote;

            private Server _server;
            private byte[] _buffer = new byte[65536];

            private IPEndPoint _localEndPoint;
            private IPEndPoint _remoteEndPoint;

            private IPAddress ListenAddress
            {
                get
                {
                    return _remote.AddressFamily switch
                    {
                        AddressFamily.InterNetwork => IPAddress.Any,
                        AddressFamily.InterNetworkV6 => IPAddress.IPv6Any,
                        _ => throw new NotSupportedException(),
                    };
                }
            }

            public UDPHandler(Socket local, Server server, IPEndPoint localEndPoint)
            {
                _local = local;
                _server = server;
                _localEndPoint = localEndPoint;

                // TODO async resolving
                bool parsed = IPAddress.TryParse(server.Host, out IPAddress ipAddress);
                if (!parsed)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(server.Host);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                _remoteEndPoint = new IPEndPoint(ipAddress, server.Port);
                _remote = new Socket(_remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _remote.Bind(new IPEndPoint(ListenAddress, 0));
            }

            public async Task SendAsync(ReadOnlyMemory<byte> data)
            {
                using ICrypto encryptor = CryptoFactory.GetEncryptor(_server.Method, _server.Password);
                using IMemoryOwner<byte> mem = pool.Rent(data.Length + 1000);

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
                        int bytesRead = result.ReceivedBytes;

                        using IMemoryOwner<byte> owner = pool.Rent(bytesRead + 3);
                        Memory<byte> o = owner.Memory;

                        using ICrypto encryptor = CryptoFactory.GetEncryptor(_server.Method, _server.Password);
                        int outlen = encryptor.DecryptUDP(o.Span[3..], _buffer.AsSpan(0, bytesRead));
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
    class LRUCache<K, V> where V : UDPRelay.UDPHandler
    {
        private int capacity;
        private Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>> cacheMap = new Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>>();
        private LinkedList<LRUCacheItem<K, V>> lruList = new LinkedList<LRUCacheItem<K, V>>();

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V get(K key)
        {
            LinkedListNode<LRUCacheItem<K, V>> node;
            if (cacheMap.TryGetValue(key, out node))
            {
                V value = node.Value.value;
                lruList.Remove(node);
                lruList.AddLast(node);
                return value;
            }
            return default(V);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void add(K key, V val)
        {
            if (cacheMap.Count >= capacity)
            {
                RemoveFirst();
            }

            LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val);
            LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
            lruList.AddLast(node);
            cacheMap.Add(key, node);
        }

        private void RemoveFirst()
        {
            // Remove from LRUPriority
            LinkedListNode<LRUCacheItem<K, V>> node = lruList.First;
            lruList.RemoveFirst();

            // Remove from cache
            cacheMap.Remove(node.Value.key);
            node.Value.value.Close();
        }
    }

    class LRUCacheItem<K, V>
    {
        public LRUCacheItem(K k, V v)
        {
            key = k;
            value = v;
        }
        public K key;
        public V value;
    }

    #endregion
}
