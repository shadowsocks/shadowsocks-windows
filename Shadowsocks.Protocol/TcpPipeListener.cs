using Pipelines.Sockets.Unofficial;
using Shadowsocks.Protocol.Shadowsocks;
using Shadowsocks.Protocol.Socks5;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol
{
    public class TcpPipeListener
    {
        private readonly TcpListener _listener;
        private readonly IEnumerable<IStreamService> _services;

        public TcpPipeListener(IPEndPoint localEP)
        {
            _listener = new TcpListener(localEP);
            _services = new[] { new Socks5Service(), };
        }

        public TcpPipeListener(IPEndPoint endPoint, IEnumerable<IStreamService> services)
        {
            _listener = new TcpListener(endPoint);
            _services = services;
        }

        public async Task Start(IPEndPoint localEP, DnsEndPoint remoteEP, string method, string? password, byte[]? key)
        {
            _listener.Start();

            while (true)
            {
                var socket = await _listener.AcceptSocketAsync();
                var conn = SocketConnection.Create(socket);

                foreach (var svc in _services)
                {
                    if (await svc.IsMyClient(conn))
                    {
                        // todo: save to list, so we can optionally close them
                        _ = RunService(svc, conn, localEP, remoteEP, method, password, key);
                    }
                }
            }
        }

        private async Task RunService(IStreamService svc, SocketConnection conn, IPEndPoint localEP, DnsEndPoint remoteEP, string method, string? password, byte[]? key)
        {
            var s5tcp = new PipePair();

            var raw = await svc.Handle(conn);
            ShadowsocksClient s5c;
            if (!string.IsNullOrEmpty(password))
                s5c = new ShadowsocksClient(method, password);
            else if (key != null)
                s5c = new ShadowsocksClient(method, key);
            else
                throw new ArgumentException("Either a password or a key must be provided.");
            var tpc = new TcpPipeClient();
            var t2 = tpc.Connect(remoteEP, s5tcp.DownSide, null);
            var t1 = s5c.Connect(localEP, raw, s5tcp.UpSide);
            await Task.WhenAll(t1, t2);
        }

        public void Stop() => _listener.Stop();
    }
}
