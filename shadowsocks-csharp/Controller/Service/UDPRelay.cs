using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Shadowsocks.Controller.Strategy;
using Shadowsocks.Encryption;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    class UDPRelay : Listener.Service
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
            Listener.UDPState udpState = (Listener.UDPState)state;
            IPEndPoint remoteEndPoint = (IPEndPoint)udpState.remoteEndPoint;
            UDPHandler handler = _cache.get(remoteEndPoint);
            if (handler == null)
            {
                handler = new UDPHandler(socket, _controller.GetAServer(IStrategyCallerType.UDP, remoteEndPoint, null/*TODO: fix this*/), remoteEndPoint);
                _cache.add(remoteEndPoint, handler);
            }
            handler.Send(firstPacket, length);
            handler.Receive();
            return true;
        }

        public class UDPHandler
        {
            private Socket _local;
            private Socket _remote;

            private Server _server;
            private byte[] _buffer = new byte[65536];

            private IPEndPoint _localEndPoint;
            private IPEndPoint _remoteEndPoint;

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
            }

            public void Send(byte[] data, int length)
            {
                IEncryptor encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                byte[] dataIn = new byte[length - 3];
                Array.Copy(data, 3, dataIn, 0, length - 3);
                byte[] dataOut = new byte[65536];  // enough space for AEAD ciphers
                int outlen;
                encryptor.EncryptUDP(dataIn, length - 3, dataOut, out outlen);
                Logging.Debug(_localEndPoint, _remoteEndPoint, outlen, "UDP Relay");
                _remote?.SendTo(dataOut, outlen, SocketFlags.None, _remoteEndPoint);
            }

            public void Receive()
            {
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Logging.Debug($"++++++Receive Server Port, size:" + _buffer.Length);
                _remote?.BeginReceiveFrom(_buffer, 0, _buffer.Length, 0, ref remoteEndPoint, new AsyncCallback(RecvFromCallback), null);
            }

            public void RecvFromCallback(IAsyncResult ar)
            {
                try
                {
                    if (_remote == null) return;
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int bytesRead = _remote.EndReceiveFrom(ar, ref remoteEndPoint);

                    byte[] dataOut = new byte[bytesRead];
                    int outlen;

                    IEncryptor encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                    encryptor.DecryptUDP(_buffer, bytesRead, dataOut, out outlen);

                    byte[] sendBuf = new byte[outlen + 3];
                    Array.Copy(dataOut, 0, sendBuf, 3, outlen);

                    Logging.Debug(_localEndPoint, _remoteEndPoint, outlen, "UDP Relay");
                    _local?.SendTo(sendBuf, outlen + 3, 0, _localEndPoint);

                    Receive();
                }
                catch (ObjectDisposedException)
                {
                    // TODO: handle the ObjectDisposedException
                }
                catch (Exception)
                {
                    // TODO: need more think about handle other Exceptions, or should remove this catch().
                }
                finally
                {
                    // No matter success or failed, we keep receiving

                }
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
