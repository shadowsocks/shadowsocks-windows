using Shadowsocks.Models;
using Shadowsocks.Net.Crypto;
using Shadowsocks.Net.Proxy;
using Splat;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Shadowsocks.Net.Crypto.CryptoBase;

namespace Shadowsocks.Net;

public class TcpRelay(Server server) : StreamService, IEnableLogger
{
    public event EventHandler<SstcpConnectedEventArgs> OnConnected;
    public event EventHandler<SsTransmitEventArgs> OnInbound;
    public event EventHandler<SsTransmitEventArgs> OnOutbound;
    public event EventHandler<SsRelayEventArgs> OnFailed;

    private DateTime _lastSweepTime = DateTime.Now;

    public ISet<TcpHandler> Handlers { get; set; } = new HashSet<TcpHandler>();

    public override bool Handle(CachedNetworkStream stream, object state)
    {

        var fp = new byte[256];
        var len = stream.ReadFirstBlock(fp);

        var socket = stream.Socket;
        if (socket.ProtocolType != ProtocolType.Tcp
            || (len < 2 || fp[0] != 5))
            return false;


        socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

        var handler = new TcpHandler(server, socket);

        IList<TcpHandler> handlersToClose = new List<TcpHandler>();
        lock (Handlers)
        {
            Handlers.Add(handler);
            var now = DateTime.Now;
            if (now - _lastSweepTime > TimeSpan.FromSeconds(1))
            {
                _lastSweepTime = now;
                foreach (var handler1 in Handlers)
                    if (now - handler1.lastActivity > TimeSpan.FromSeconds(900))
                        handlersToClose.Add(handler1);
            }
        }
        foreach (var handler1 in handlersToClose)
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
        var handler = new TcpHandler(server, socket);

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

        IList<TcpHandler> handlersToClose = new List<TcpHandler>();
        lock (Handlers)
        {
            Handlers.Add(handler);
            var now = DateTime.Now;
            if (now - _lastSweepTime > TimeSpan.FromSeconds(1))
            {
                _lastSweepTime = now;
                foreach (var handler1 in Handlers)
                {
                    if (now - handler1.lastActivity > TimeSpan.FromSeconds(900))
                    {
                        handlersToClose.Add(handler1);
                    }
                }
            }
        }
        foreach (TcpHandler handler1 in handlersToClose)
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
        List<TcpHandler> handlersToClose = [];
        lock (Handlers)
        {
            handlersToClose.AddRange(Handlers);
        }
        handlersToClose.ForEach(h => h.Close());
    }
}

public class SsRelayEventArgs(Server server) : EventArgs
{
    public readonly Server server = server;
}

public class SsTransmitEventArgs(Server server, long length) : SsRelayEventArgs(server)
{
    public readonly long length = length;
}

public class SstcpConnectedEventArgs(Server server, TimeSpan latency) : SsRelayEventArgs(server)
{
    public readonly TimeSpan latency = latency;
}

public class TcpHandler(Server server, Socket socket) : IEnableLogger
{
    public event EventHandler<SstcpConnectedEventArgs> OnConnected;
    public event EventHandler<SsTransmitEventArgs> OnInbound;
    public event EventHandler<SsTransmitEventArgs> OnOutbound;
    public event EventHandler<SsRelayEventArgs> OnClosed;
    public event EventHandler<SsRelayEventArgs> OnFailed;

    private readonly int _serverTimeout = 5000;
    private readonly int _proxyTimeout = 5000;

    private readonly MemoryPool<byte> pool = MemoryPool<byte>.Shared;
    // each recv size.
    public const int RecvSize = 16384;

    // overhead of one chunk, reserved for AEAD ciphers
    public const int ChunkOverheadSize = 100;//16 * 2 /* two tags */ + AEADEncryptor.ChunkLengthBytes;

    // In general, the ciphertext length, we should take overhead into account
    public const int SendSize = 32768;

    public DateTime lastActivity = DateTime.Now;

    // TODO: forward proxy
    //private readonly ForwardProxyConfig _config;
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
    private readonly object _encryptionLock = new();
    private readonly object _decryptionLock = new();
    private readonly object _closeConnLock = new();

    // TODO: decouple controller

    public void CreateRemote(EndPoint destination)
    {
        if (server == null || server.Host == "")
        {
            throw new ArgumentException("No server configured");
        }

        encryptor = CryptoFactory.GetEncryptor(server.Method, server.Password);
        decryptor = CryptoFactory.GetEncryptor(server.Method, server.Password);
    }

    public async Task StartAsync(byte[] firstPacket, int length)
    {
        _firstPacket = firstPacket;
        _firstPacketLength = length;
        var (cmd, dst) = await Socks5Handshake();
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

        OnClosed?.Invoke(this, new SsRelayEventArgs(server));

        try
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

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
        await socket.SendAsync(response, SocketFlags.None);

        using var bufOwner = pool.Rent(512);
        var buf = bufOwner.Memory;

        if (await socket.ReceiveAsync(buf.Slice(0, 5), SocketFlags.None) != 5)
        {
            Close();
            return (0, default);
        }

        var cmd = buf.Span[1];
        EndPoint dst = default;
        switch (cmd)
        {
            case CMD_CONNECT:
                await socket.SendAsync(new byte[] { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 }, SocketFlags.None);
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

    private async Task DrainConnection()
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
                l = await socket.ReceiveAsync(b.Memory, SocketFlags.None);
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

        var toRead = atyp switch
        {
            ATYP_IPv4 => 4,
            ATYP_IPv6 => 16,
            ATYP_DOMAIN => maybeDomainLength + 1,
            _ => throw new NotSupportedException(),
        } + 2 - 1;
        await socket.ReceiveAsync(buf.Slice(2, toRead), SocketFlags.None);

        return GetSocks5EndPoint(buf.ToArray());
    }

    private int ReadPort(byte[] arr, long offset) => (arr[offset] << 8) + arr[offset + 1];

    private EndPoint GetSocks5EndPoint(byte[] buf)
    {
        var maybeDomainLength = buf[1] + 2;

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
        var endPoint = (IPEndPoint)socket.LocalEndPoint;
        var address = endPoint.Address.GetAddressBytes();
        var port = endPoint.Port;
        var response = new byte[4 + address.Length + ADDR_PORT_LEN];
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
        await socket.SendAsync(response, SocketFlags.None);
    }

    private async Task ConnectRemote(EndPoint destination)
    {
        CreateRemote(destination);
        EndPoint proxyEP = null;
        EndPoint serverEP = new DnsEndPoint(server.Host, server.Port);
        EndPoint pluginEP = null; // TODO: plugin local end point

        IProxy remote = new DirectConnect(); // TODO: forward proxy
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

        var cancelProxy = new CancellationTokenSource(_proxyTimeout * 1000);

        await remote.ConnectProxyAsync(proxyEP, auth, cancelProxy.Token);
        _remote = remote;

        if (!(remote is DirectConnect))
        {
            this.Log().Debug($"Socket connected to proxy {remote.ProxyEndPoint}");
        }

        var startConnectTime = DateTime.Now;
        var cancelServer = new CancellationTokenSource(_serverTimeout * 1000);
        await remote.ConnectRemoteAsync(serverEP, cancelServer.Token);
        this.Log().Debug($"Socket connected to ss server: {server}");
        var latency = DateTime.Now - startConnectTime;
        OnConnected?.Invoke(this, new SstcpConnectedEventArgs(server, latency));

    }

    private async Task SendAddress(EndPoint dest)
    {
        var dstByte = GetSocks5EndPointByte(dest);
        using var t = pool.Rent(512);
        try
        {
            var addrlen = encryptor.Encrypt(dstByte, t.Memory.Span);
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
            var r = new byte[d.Host.Length + 4];
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
                var r = new byte[7];
                r[0] = 1;
                i.Address.GetAddressBytes().CopyTo(r, 1);
                r[^2] = (byte)(i.Port / 256);
                r[^1] = (byte)(i.Port % 256);
                return r;
            }
            else if (i.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var r = new byte[19];
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
                var len = await _remote.ReceiveAsync(cipher);
                if (len == 0) break;
                var plen = decryptor.Decrypt(plain.Span, cipher.Span.Slice(0, len));
                if (plen == 0) continue;
                var len2 = await socket.SendAsync(plain.Slice(0, plen), SocketFlags.None);
                if (len2 == 0) break;
                OnInbound?.Invoke(this, new SsTransmitEventArgs(server, plen));
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
            var len = await socket.ReceiveAsync(plain, SocketFlags.None);

            if (len == 0) break;
            var clen = encryptor.Encrypt(plain.Span.Slice(0, len), cipher.Span);
            var len2 = await _remote.SendAsync(cipher.Slice(0, clen));
            if (len2 == 0) break;
            OnOutbound?.Invoke(this, new SsTransmitEventArgs(server, len));
        }
        _remote.Shutdown(SocketShutdown.Send);
    }
}