using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Shadowsocks.Net
{
    public interface IStreamService
    {
        [Obsolete]
        bool Handle(byte[] firstPacket, int length, Socket socket, object state);

        public abstract bool Handle(CachedNetworkStream stream, object state);

        void Stop();
    }

    public abstract class StreamService : IStreamService
    {
        [Obsolete]
        public abstract bool Handle(byte[] firstPacket, int length, Socket socket, object state);

        public abstract bool Handle(CachedNetworkStream stream, object state);

        public virtual void Stop() { }
    }

    public class TCPListener : IEnableLogger
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
        Socket _tcpSocket;
        IEnumerable<IStreamService> _services;

        public TCPListener(IPEndPoint localEndPoint, IEnumerable<IStreamService> services)
        {
            _localEndPoint = localEndPoint;
            _services = services;
        }

        private bool CheckIfPortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            return ipProperties.GetActiveTcpListeners().Any(endPoint => endPoint.Port == port);
        }

        public void Start()
        {
            if (CheckIfPortInUse(_localEndPoint.Port))
                throw new Exception($"Port {_localEndPoint.Port} already in use");

            try
            {
                // Create a TCP/IP socket.
                _tcpSocket = new Socket(_localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _tcpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                // Bind the socket to the local endpoint and listen for incoming connections.
                _tcpSocket.Bind(_localEndPoint);
                _tcpSocket.Listen(1024);

                // Start an asynchronous socket to listen for connections.
                this.Log().Info($"Shadowsocks started TCP");
                this.Log().Debug(Crypto.CryptoFactory.DumpRegisteredEncryptor());
                _tcpSocket.BeginAccept(new AsyncCallback(AcceptCallback), _tcpSocket);
            }
            catch (SocketException)
            {
                _tcpSocket.Close();
                throw;
            }
        }

        public void Stop()
        {
            _tcpSocket?.Close();

            foreach (IStreamService s in _services)
            {
                s.Stop();
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                Socket conn = listener.EndAccept(ar);

                byte[] buf = new byte[4096];
                object[] state = new object[] {
                    conn,
                    buf
                };

                conn.BeginReceive(buf, 0, buf.Length, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                this.Log().Error(e, "");
            }
            finally
            {
                try
                {
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (Exception e)
                {
                    this.Log().Error(e, "");
                }
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;

            Socket conn = (Socket)state[0];
            byte[] buf = (byte[])state[1];
            try
            {
                int bytesRead = conn.EndReceive(ar);
                if (bytesRead <= 0)
                {
                    goto Shutdown;
                }

                foreach (IStreamService service in _services)
                {
                    if (service.Handle(buf, bytesRead, conn, null))
                    {
                        return;
                    }
                }
            Shutdown:
                // no service found for this
                if (conn.ProtocolType == ProtocolType.Tcp)
                {
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                this.Log().Error(e, "");
                conn.Close();
            }
        }
    }
}
