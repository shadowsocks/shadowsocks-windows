using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Net
{
    public interface IDatagramService
    {
        public abstract Task< bool> Handle(Memory<byte> packet, Socket socket, EndPoint client);

        void Stop();
    }

    public abstract class DatagramService : IDatagramService
    {
        public abstract Task<bool> Handle(Memory<byte> packet, Socket socket, EndPoint client);

        public virtual void Stop() { }
    }

    public class UDPListener : IEnableLogger
    {
        public class UDPState
        {
            public UDPState(Socket s)
            {
                socket = s;
                remoteEndPoint = new IPEndPoint(s.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            }
            public Socket socket;
            public byte[] buffer = new byte[4096];
            public EndPoint remoteEndPoint;
        }

        IPEndPoint _localEndPoint;
        Socket _udpSocket;
        IEnumerable<IDatagramService> _services;
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        public UDPListener(IPEndPoint localEndPoint, IEnumerable<IDatagramService> services)
        {
            _localEndPoint = localEndPoint;
            _services = services;
        }

        private bool CheckIfPortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            return ipProperties.GetActiveUdpListeners().Any(endPoint => endPoint.Port == port);
        }

        public void Start()
        {
            if (CheckIfPortInUse(_localEndPoint.Port))
                throw new Exception($"Port {_localEndPoint.Port} already in use");

            // Create a TCP/IP socket.
            _udpSocket = new Socket(_localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

            // Bind the socket to the local endpoint and listen for incoming connections.
            _udpSocket.Bind(_localEndPoint);

            // Start an asynchronous socket to listen for connections.
            this.Log().Info($"Shadowsocks started UDP");
            this.Log().Debug(Crypto.CryptoFactory.DumpRegisteredEncryptor());
            UDPState udpState = new UDPState(_udpSocket);
            // _udpSocket.BeginReceiveFrom(udpState.buffer, 0, udpState.buffer.Length, 0, ref udpState.remoteEndPoint, new AsyncCallback(RecvFromCallback), udpState);
            Task.Run(() => WorkLoop(tokenSource.Token));
        }

        private async Task WorkLoop(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            EndPoint remote = new IPEndPoint(_udpSocket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            while (!token.IsCancellationRequested)
            {
                var result = await _udpSocket.ReceiveFromAsync(buffer, SocketFlags.None, remote);
                var len = result.ReceivedBytes;
                foreach (IDatagramService service in _services)
                {
                    if (await service.Handle(new Memory<byte>(buffer)[..len], _udpSocket, result.RemoteEndPoint))
                    {
                        break;
                    }
                }
            }
        }

        public void Stop()
        {
            tokenSource.Cancel();
            _udpSocket?.Close();
            foreach (var s in _services)
            {
                s.Stop();
            }
        }
    }
}
