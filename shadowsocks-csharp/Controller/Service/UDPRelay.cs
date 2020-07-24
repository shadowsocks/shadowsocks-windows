using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NLog;
using Shadowsocks.Controller.Strategy;
using Shadowsocks.Encryption;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    class UDPRelay : DatagramService
    {
        private ShadowsocksController _controller;

        // TODO: choose a smart number
        private LRUCache<IPEndPoint, UDPHandler> _cache = new LRUCache<IPEndPoint, UDPHandler>(512);

        public long outbound = 0;
        public long inbound = 0;

        public UDPRelay(ShadowsocksController controller)
        {
            this._controller = controller;
        }

        // TODO: UDP is datagram protocol not stream protocol
        public override bool Handle(CachedNetworkStream stream, object state)
        {
            byte[] fp = new byte[256];
            int len = stream.ReadFirstBlock(fp);
            return Handle(fp, len, stream.Socket, state);
        }

        [Obsolete]
        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Udp)
            {
                return false;
            }
            if (length < 4)
            {
                return false;
            }
            // UDPListener.UDPState udpState = (UDPListener.UDPState)state;
            IPEndPoint remoteEndPoint = (IPEndPoint)state;
            // IPEndPoint remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            UDPHandler handler = _cache.get(remoteEndPoint);
            if (handler == null)
            {
                handler = new UDPHandler(socket, _controller.GetAServer(IStrategyCallerType.UDP, remoteEndPoint, null/*TODO: fix this*/), remoteEndPoint);
                handler.Receive();
                _cache.add(remoteEndPoint, handler);
            }
            handler.Send(firstPacket, length);
            return true;
        }

        public class UDPHandler
        {
            private static Logger logger = LogManager.GetCurrentClassLogger();

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
                IPAddress ipAddress;
                bool parsed = IPAddress.TryParse(server.server, out ipAddress);
                if (!parsed)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(server.server);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                _remoteEndPoint = new IPEndPoint(ipAddress, server.server_port);
                _remote = new Socket(_remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _remote.Bind(new IPEndPoint(ListenAddress, 0));
            }

            public void Send(byte[] data, int length)
            {
                IEncryptor encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                var slicedData = data.AsSpan(0, length);
                byte[] dataOut = new byte[slicedData.Length + 1000];
                var dataToSend = slicedData[3..];
                int outlen = encryptor.EncryptUDP(slicedData[3..], dataOut);
                logger.Debug(_localEndPoint, _remoteEndPoint, outlen, "UDP Relay up");
                _remote?.SendTo(dataOut, outlen, SocketFlags.None, _remoteEndPoint);
            }

            public async Task ReceiveAsync()
            {
                EndPoint remoteEndPoint = new IPEndPoint(ListenAddress, 0);
                logger.Debug($"++++++Receive Server Port, size:" + _buffer.Length);
                try
                {

                    while (true)
                    {

                        var result = await _remote.ReceiveFromAsync(_buffer, SocketFlags.None, remoteEndPoint);
                        int bytesRead = result.ReceivedBytes;
                        byte[] dataOut = new byte[bytesRead];
                        int outlen;

                        IEncryptor encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                        outlen = encryptor.DecryptUDP(dataOut, _buffer.AsSpan(0, bytesRead));
                        byte[] sendBuf = new byte[outlen + 3];
                        Array.Copy(dataOut, 0, sendBuf, 3, outlen);

                        logger.Debug(_remoteEndPoint, _localEndPoint, outlen, "UDP Relay down");
                        await _local?.SendToAsync(sendBuf, SocketFlags.None, _localEndPoint);
                    }
                }
                catch (Exception e)
                {
                    logger.LogUsefulException(e);
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
