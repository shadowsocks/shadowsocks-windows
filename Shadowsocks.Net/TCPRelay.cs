using Splat;
using Shadowsocks.Net.Crypto;
using Shadowsocks.Net.Crypto.AEAD;
using Shadowsocks.Net.Proxy;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static Shadowsocks.Net.Crypto.CryptoBase;
using Shadowsocks.Models;

namespace Shadowsocks.Net
{
    class TCPRelay : StreamService, IEnableLogger
    {
        public event EventHandler<SSTCPConnectedEventArgs> OnConnected;
        public event EventHandler<SSTransmitEventArgs> OnInbound;
        public event EventHandler<SSTransmitEventArgs> OnOutbound;
        public event EventHandler<SSRelayEventArgs> OnFailed;

        private Server _server;
        private DateTime _lastSweepTime;

        public ISet<TCPHandler> Handlers { get; set; }

        public TCPRelay(Server server)
        {
            _server = server;
            Handlers = new HashSet<TCPHandler>();
            _lastSweepTime = DateTime.Now;
        }

        public override bool Handle(CachedNetworkStream stream, object state)
        {

            byte[] fp = new byte[256];
            int len = stream.ReadFirstBlock(fp);

            var socket = stream.Socket;
            if (socket.ProtocolType != ProtocolType.Tcp
                || (len < 2 || fp[0] != 5))
                return false;


            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            TCPHandler handler = new TCPHandler(_server, socket);

            IList<TCPHandler> handlersToClose = new List<TCPHandler>();
            lock (Handlers)
            {
                Handlers.Add(handler);
                DateTime now = DateTime.Now;
                if (now - _lastSweepTime > TimeSpan.FromSeconds(1))
                {
                    _lastSweepTime = now;
                    foreach (TCPHandler handler1 in Handlers)
                        if (now - handler1.lastActivity > TimeSpan.FromSeconds(900))
                            handlersToClose.Add(handler1);
                }
            }
            foreach (TCPHandler handler1 in handlersToClose)
            {
                this.Log().Debug("Closing timed out TCP connection.");
                handler1.Close();
            }

            /*
             * Start after we put it into Handlers set. Otherwise if it failed in handler.Start()
             * then it will call handler.Close() before we add it into the set.
             * Then the handler will never release until the next Handle call. Sometimes it will
             * cause odd problems (especially during memory profiling).
             */
            // handler.Start(fp, len);
            _ = handler.StartAsync(fp, len);

            return true;
            // return Handle(fp, len, stream.Socket, state);
        }

        [Obsolete]
        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Tcp
                || (length < 2 || firstPacket[0] != 5))
            {
                return false;
            }

            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            TCPHandler handler = new TCPHandler(_server, socket);

            handler.OnConnected += OnConnected;
            handler.OnInbound += OnInbound;
            handler.OnOutbound += OnOutbound;
            handler.OnFailed += OnFailed;
            handler.OnClosed += (h, arg) =>
            {
                lock (Handlers)
                {
                    Handlers.Remove(handler);
                }
            };

            IList<TCPHandler> handlersToClose = new List<TCPHandler>();
            lock (Handlers)
            {
                Handlers.Add(handler);
                DateTime now = DateTime.Now;
                if (now - _lastSweepTime > TimeSpan.FromSeconds(1))
                {
                    _lastSweepTime = now;
                    foreach (TCPHandler handler1 in Handlers)
                    {
                        if (now - handler1.lastActivity > TimeSpan.FromSeconds(900))
                        {
                            handlersToClose.Add(handler1);
                        }
                    }
                }
            }
            foreach (TCPHandler handler1 in handlersToClose)
            {
                this.Log().Debug("Closing timed out TCP connection.");
                handler1.Close();
            }

            /*
             * Start after we put it into Handlers set. Otherwise if it failed in handler.Start()
             * then it will call handler.Close() before we add it into the set.
             * Then the handler will never release until the next Handle call. Sometimes it will
             * cause odd problems (especially during memory profiling).
             */
            // handler.Start(firstPacket, length);
            _ = handler.StartAsync(firstPacket, length);

            return true;
        }

        public override void Stop()
        {
            List<TCPHandler> handlersToClose = new List<TCPHandler>();
            lock (Handlers)
            {
                handlersToClose.AddRange(Handlers);
            }
            handlersToClose.ForEach(h => h.Close());
        }
    }

    public class SSRelayEventArgs : EventArgs
    {
        public readonly Server server;

        public SSRelayEventArgs(Server server)
        {
            this.server = server;
        }
    }

    public class SSTransmitEventArgs : SSRelayEventArgs
    {
        public readonly long length;
        public SSTransmitEventArgs(Server server, long length) : base(server)
        {
            this.length = length;
        }
    }

    public class SSTCPConnectedEventArgs : SSRelayEventArgs
    {
        public readonly TimeSpan latency;

        public SSTCPConnectedEventArgs(Server server, TimeSpan latency) : base(server)
        {
            this.latency = latency;
        }
    }

    internal class TCPHandler : IEnableLogger
    {
        public event EventHandler<SSTCPConnectedEventArgs> OnConnected;
        public event EventHandler<SSTransmitEventArgs> OnInbound;
        public event EventHandler<SSTransmitEventArgs> OnOutbound;
        public event EventHandler<SSRelayEventArgs> OnClosed;
        public event EventHandler<SSRelayEventArgs> OnFailed;

        private readonly int _serverTimeout;
        private readonly int _proxyTimeout;

        private readonly MemoryPool<byte> pool = MemoryPool<byte>.Shared;
        // each recv size.
        public const int RecvSize = 16384;

        // overhead of one chunk, reserved for AEAD ciphers
        public const int ChunkOverheadSize = 100;//16 * 2 /* two tags */ + AEADEncryptor.ChunkLengthBytes;

        // In general, the ciphertext length, we should take overhead into account
        public const int SendSize = 32768;

        public DateTime lastActivity;

        // TODO: forward proxy
        //private readonly ForwardProxyConfig _config;
        private readonly Server _server;
        private readonly Socket _connection;
        private IProxy _remote;
        private ICrypto encryptor;
        // workaround
        private ICrypto decryptor;

        private byte[] _firstPacket;
        private int _firstPacketLength;

        private const int CMD_CONNECT = 0x01;
        private const int CMD_BIND = 0x02;
        private const int CMD_UDP_ASSOC = 0x03;

        private bool _closed = false;

        // instance-based lock without static
        private readonly object _encryptionLock = new object();
        private readonly object _decryptionLock = new object();
        private readonly object _closeConnLock = new object();

        // TODO: decouple controller
        public TCPHandler(Server server, Socket socket)
        {
            _server = server;
            _connection = socket;
            _proxyTimeout = 5000;
            _serverTimeout = 5000;

            lastActivity = DateTime.Now;
        }

        public void CreateRemote(EndPoint destination)
        {
            if (_server == null || _server.Host == "")
            {
                throw new ArgumentException("No server configured");
            }

            encryptor = CryptoFactory.GetEncryptor(_server.Method, _server.Password);
            decryptor = CryptoFactory.GetEncryptor(_server.Method, _server.Password);
        }

        public async Task StartAsync(byte[] firstPacket, int length)
        {
            _firstPacket = firstPacket;
            _firstPacketLength = length;
            (int cmd, EndPoint dst) = await Socks5Handshake();
            if (cmd == CMD_CONNECT)
            {
                await ConnectRemote(dst);
                await SendAddress(dst);
                await Forward();
            }
            else if (cmd == CMD_UDP_ASSOC)
            {
                await DrainConnection();
            }
        }

        private void ErrorClose(Exception e)
        {
            this.Log().Error(e, "");
            Close();
        }

        public void Close()
        {
            lock (_closeConnLock)
            {
                if (_closed)
                {
                    return;
                }

                _closed = true;
            }

            OnClosed?.Invoke(this, new SSRelayEventArgs(_server));

            try
            {
                _connection.Shutdown(SocketShutdown.Both);
                _connection.Close();

                encryptor?.Dispose();
                decryptor?.Dispose();
            }
            catch (Exception e)
            {
                this.Log().Error(e, "");
            }
        }

        async Task<(int cmd, EndPoint destination)> Socks5Handshake()
        {
            // not so strict here
            // 5 2 1 2 should return 5 255
            // 5 1 0 5 / 1 0 1 127 0 0 1 0 80 will cause handshake fail

            int bytesRead = _firstPacketLength;
            if (bytesRead <= 1)
            {
                Close();
                return (0, default);
            }

            byte[] response = { 5, 0 };
            if (_firstPacket[0] != 5)
            {
                // reject socks 4
                response = new byte[] { 0, 91 };
                this.Log().Error("socks5 protocol error");
            }
            await _connection.SendAsync(response, SocketFlags.None);

            using var bufOwner = pool.Rent(512);
            var buf = bufOwner.Memory;

            if (await _connection.ReceiveAsync(buf.Slice(0, 5), SocketFlags.None) != 5)
            {
                Close();
                return (0, default);
            }

            var cmd = buf.Span[1];
            EndPoint dst = default;
            switch (cmd)
            {
                case CMD_CONNECT:
                    await _connection.SendAsync(new byte[] { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 }, SocketFlags.None);
                    dst = await ReadAddress(buf);
                    // start forward
                    break;
                case CMD_UDP_ASSOC:
                    dst = await ReadAddress(buf);
                    await SendUdpAssociate();
                    // drain
                    break;
                default:
                    Close();
                    break;
            }
            return (cmd, dst);
        }

        async Task DrainConnection()
        {
            if (_closed)
            {
                return;
            }
            using var b = pool.Rent(512);
            try
            {
                int l;
                do
                {
                    l = await _connection.ReceiveAsync(b.Memory, SocketFlags.None);
                }
                while (l > 0);

                Close();
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private async Task<EndPoint> ReadAddress(Memory<byte> buf)
        {
            var atyp = buf.Span[3];
            var maybeDomainLength = buf.Span[4];
            buf.Span[0] = atyp;
            buf.Span[1] = maybeDomainLength;

            int toRead = atyp switch
            {
                ATYP_IPv4 => 4,
                ATYP_IPv6 => 16,
                ATYP_DOMAIN => maybeDomainLength + 1,
                _ => throw new NotSupportedException(),
            } + 2 - 1;
            await _connection.ReceiveAsync(buf.Slice(2, toRead), SocketFlags.None);

            return GetSocks5EndPoint(buf.ToArray());
        }

        private int ReadPort(byte[] arr, long offset)
        {
            return (arr[offset] << 8) + arr[offset + 1];
        }

        private EndPoint GetSocks5EndPoint(byte[] buf)
        {
            int maybeDomainLength = buf[1] + 2;

            return (buf[0]) switch
            {
                ATYP_IPv4 => new IPEndPoint(new IPAddress(buf[1..5]), ReadPort(buf, 5)),
                ATYP_IPv6 => new IPEndPoint(new IPAddress(buf[1..17]), ReadPort(buf, 17)),
                ATYP_DOMAIN => new DnsEndPoint(Encoding.ASCII.GetString(buf[2..maybeDomainLength]), ReadPort(buf, maybeDomainLength)),
                _ => throw new NotSupportedException(),
            };
        }

        private async Task SendUdpAssociate()
        {
            IPEndPoint endPoint = (IPEndPoint)_connection.LocalEndPoint;
            byte[] address = endPoint.Address.GetAddressBytes();
            int port = endPoint.Port;
            byte[] response = new byte[4 + address.Length + ADDR_PORT_LEN];
            response[0] = 5;
            switch (endPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    response[3] = ATYP_IPv4;
                    break;
                case AddressFamily.InterNetworkV6:
                    response[3] = ATYP_IPv6;
                    break;
            }
            address.CopyTo(response, 4);
            response[^1] = (byte)(port & 0xFF);
            response[^2] = (byte)((port >> 8) & 0xFF);
            await _connection.SendAsync(response, SocketFlags.None);
        }

        private async Task ConnectRemote(EndPoint destination)
        {
            CreateRemote(destination);
            IProxy remote;
            EndPoint proxyEP = null;
            EndPoint serverEP = new DnsEndPoint(_server.Host, _server.Port);
            EndPoint pluginEP = null; // TODO: plugin local end point

            remote = new DirectConnect(); // TODO: forward proxy
            NetworkCredential auth = null;
            /*if (_config.useAuth)
            {
                auth = new NetworkCredential(_config.authUser, _config.authPwd);
            }
            if (pluginEP != null)
            {
                serverEP = pluginEP;
                remote = new DirectConnect();
            }
            else if (_config.useProxy)
            {
                remote = _config.proxyType switch
                {
                    ForwardProxyConfig.PROXY_SOCKS5 => new Socks5Proxy(),
                    ForwardProxyConfig.PROXY_HTTP => new HttpProxy(),
                    _ => throw new NotSupportedException("Unknown forward proxy."),
                };
                proxyEP = new DnsEndPoint(_config.proxyServer, _config.proxyPort);
            }
            else
            {
                remote = new DirectConnect();
            }*/

            CancellationTokenSource cancelProxy = new CancellationTokenSource(_proxyTimeout * 1000);

            await remote.ConnectProxyAsync(proxyEP, auth, cancelProxy.Token);
            _remote = remote;

            if (!(remote is DirectConnect))
            {
                this.Log().Debug($"Socket connected to proxy {remote.ProxyEndPoint}");
            }

            var _startConnectTime = DateTime.Now;
            CancellationTokenSource cancelServer = new CancellationTokenSource(_serverTimeout * 1000);
            await remote.ConnectRemoteAsync(serverEP, cancelServer.Token);
            this.Log().Debug($"Socket connected to ss server: {_server}");
            TimeSpan latency = DateTime.Now - _startConnectTime;
            OnConnected?.Invoke(this, new SSTCPConnectedEventArgs(_server, latency));

        }

        private async Task SendAddress(EndPoint dest)
        {
            byte[] dstByte = GetSocks5EndPointByte(dest);
            using var t = pool.Rent(512);
            try
            {
                int addrlen = encryptor.Encrypt(dstByte, t.Memory.Span);
                await _remote.SendAsync(t.Memory.Slice(0, addrlen));
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private byte[] GetSocks5EndPointByte(EndPoint dest)
        {
            if (dest is DnsEndPoint d)
            {
                byte[] r = new byte[d.Host.Length + 4];
                r[0] = 3;
                r[1] = (byte)d.Host.Length;
                Encoding.ASCII.GetBytes(d.Host, r.AsSpan(2));
                r[^2] = (byte)(d.Port / 256);
                r[^1] = (byte)(d.Port % 256);
                return r;
            }
            else if (dest is IPEndPoint i)
            {
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] r = new byte[7];
                    r[0] = 1;
                    i.Address.GetAddressBytes().CopyTo(r, 1);
                    r[^2] = (byte)(i.Port / 256);
                    r[^1] = (byte)(i.Port % 256);
                    return r;
                }
                else if (i.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    byte[] r = new byte[19];
                    r[0] = 1;
                    i.Address.GetAddressBytes().CopyTo(r, 1);
                    r[^2] = (byte)(i.Port / 256);
                    r[^1] = (byte)(i.Port % 256);
                    return r;
                }
            }
            throw new NotImplementedException();
        }

        private async Task Forward()
        {
            try
            {
                await Task.WhenAll(ForwardInbound(), ForwardOutbound());
                Close();
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private async Task ForwardInbound()
        {
            using var cipherOwner = pool.Rent(RecvSize);
            using var plainOwner = pool.Rent(SendSize);
            var plain = plainOwner.Memory;
            var cipher = cipherOwner.Memory;
            try
            {

                while (true)
                {
                    int len = await _remote.ReceiveAsync(cipher);
                    if (len == 0) break;
                    int plen = decryptor.Decrypt(plain.Span, cipher.Span.Slice(0, len));
                    if (plen == 0) continue;
                    int len2 = await _connection.SendAsync(plain.Slice(0, plen), SocketFlags.None);
                    if (len2 == 0) break;
                    OnInbound?.Invoke(this, new SSTransmitEventArgs(_server, plen));
                }
            }
            catch (Exception e)
            {
                ErrorClose(e);
            }
        }

        private async Task ForwardOutbound()
        {
            using var plainOwner = pool.Rent(RecvSize);
            using var cipherOwner = pool.Rent(SendSize);
            var plain = plainOwner.Memory;
            var cipher = cipherOwner.Memory;
            while (true)
            {
                int len = await _connection.ReceiveAsync(plain, SocketFlags.None);

                if (len == 0) break;
                int clen = encryptor.Encrypt(plain.Span.Slice(0, len), cipher.Span);
                int len2 = await _remote.SendAsync(cipher.Slice(0, clen));
                if (len2 == 0) break;
                OnOutbound?.Invoke(this, new SSTransmitEventArgs(_server, len));
            }
            _remote.Shutdown(SocketShutdown.Send);
        }
    }
}