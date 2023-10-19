using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Net;

public interface IDatagramService
{
    public abstract Task<bool> Handle(Memory<byte> packet, Socket socket, EndPoint client);

    void Stop();
}

public abstract class DatagramService : IDatagramService
{
    public abstract Task<bool> Handle(Memory<byte> packet, Socket socket, EndPoint client);

    public virtual void Stop() { }
}

public class UdpListener(IPEndPoint localEndPoint, IEnumerable<IDatagramService> services)
    : IEnableLogger
{
    public class UdpState(Socket s)
    {
        public Socket socket = s;
        public byte[] buffer = new byte[4096];
        public EndPoint remoteEndPoint = new IPEndPoint(s.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
    }

    private Socket _udpSocket;
    private readonly CancellationTokenSource _tokenSource = new();

    private bool CheckIfPortInUse(int port)
    {
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        return ipProperties.GetActiveUdpListeners().Any(endPoint => endPoint.Port == port);
    }

    public void Start()
    {
        if (CheckIfPortInUse(localEndPoint.Port))
            throw new Exception($"Port {localEndPoint.Port} already in use");

        // Create a TCP/IP socket.
        _udpSocket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

        // Bind the socket to the local endpoint and listen for incoming connections.
        _udpSocket.Bind(localEndPoint);

        // Start an asynchronous socket to listen for connections.
        this.Log().Info($"Shadowsocks started UDP");
        this.Log().Debug(Crypto.CryptoFactory.DumpRegisteredEncryptor());
        var udpState = new UdpState(_udpSocket);
        // _udpSocket.BeginReceiveFrom(udpState.buffer, 0, udpState.buffer.Length, 0, ref udpState.remoteEndPoint, new AsyncCallback(RecvFromCallback), udpState);
        Task.Run(() => WorkLoop(_tokenSource.Token));
    }

    private async Task WorkLoop(CancellationToken token)
    {
        var buffer = new byte[4096];
        EndPoint remote = new IPEndPoint(_udpSocket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
        while (!token.IsCancellationRequested)
        {
            var result = await _udpSocket.ReceiveFromAsync(buffer, SocketFlags.None, remote);
            var len = result.ReceivedBytes;
            foreach (var service in services)
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
        _tokenSource.Cancel();
        _udpSocket?.Close();
        foreach (var service in services)
        {
            service.Stop();
        }
    }
}