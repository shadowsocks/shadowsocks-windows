using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    public class Listener
    {
        public interface IService
        {
            bool Handle(byte[] firstPacket, int length, Socket socket, object state);

            void Stop();
        }

        public abstract class Service : IService
        {
            public abstract bool Handle(byte[] firstPacket, int length, Socket socket, object state);

            public virtual void Stop() { }
        }

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

        Configuration _config;
        Socket _tcpSocket;
        Socket _udpSocket;
        List<IService> _services;

        public Listener(List<IService> services)
        {
            this._services = services;
        }

        private bool CheckIfPortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            return ipProperties.GetActiveTcpListeners().Any(endPoint => endPoint.Port == port);
        }

        public void Start(Configuration config)
        {
            this._config = config;

            if (CheckIfPortInUse(_config.localPort))
                throw new Exception(I18N.GetString("Port {0} already in use", _config.localPort));

            try
            {
                // Prepare listening port
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.IPv6Any, _config.localPort);

                // Create a dual-stack TCP/IP socket. Supported from Windows Vista and above.
                _tcpSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _tcpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                _tcpSocket.Bind(localEndPoint);
                _tcpSocket.Listen(1024);

                _udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                _udpSocket.Bind(localEndPoint);

                // Start an asynchronous socket to listen for connections.
                Logging.Info($"Shadowsocks started ({UpdateChecker.Version})");
                if (_config.isVerboseLogging)
                {
                    Logging.Info(Encryption.EncryptorFactory.DumpRegisteredEncryptor());
                }
                _tcpSocket.BeginAccept(new AsyncCallback(AcceptCallback), _tcpSocket);
                UDPState udpState = new UDPState(_udpSocket);
                _udpSocket.BeginReceiveFrom(udpState.buffer, 0, udpState.buffer.Length, 0, ref udpState.remoteEndPoint, new AsyncCallback(RecvFromCallback), udpState);
            }
            catch (SocketException)
            {
                _tcpSocket.Close();
                throw;
            }
        }

        public void Stop()
        {
            if (_tcpSocket != null)
            {
                _tcpSocket.Close();
                _tcpSocket = null;
            }
            if (_udpSocket != null)
            {
                _udpSocket.Close();
                _udpSocket = null;
            }

            _services.ForEach(s => s.Stop());
        }

        public void RecvFromCallback(IAsyncResult ar)
        {
            UDPState state = (UDPState)ar.AsyncState;
            var socket = state.socket;

            try
            {
                IPAddress remoteIpAddress = ((IPEndPoint)socket.RemoteEndPoint).Address;
                if (!_config.shareOverLan && !remoteIpAddress.Equals(IPAddress.IPv6Loopback) && !remoteIpAddress.Equals(IPAddress.Loopback.MapToIPv6()))
                {
                    return;
                }
                int bytesRead = socket.EndReceiveFrom(ar, ref state.remoteEndPoint);
                foreach (IService service in _services)
                {
                    if (service.Handle(state.buffer, bytesRead, socket, state))
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Logging.Debug(ex);
            }
            finally
            {
                try
                {
                    socket.BeginReceiveFrom(state.buffer, 0, state.buffer.Length, 0, ref state.remoteEndPoint, new AsyncCallback(RecvFromCallback), state);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (Exception)
                {
                }
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                Socket conn = listener.EndAccept(ar);
                IPAddress remoteIpAddress = ((IPEndPoint)conn.RemoteEndPoint).Address;
                if (!_config.shareOverLan && !remoteIpAddress.Equals(IPAddress.IPv6Loopback) && !remoteIpAddress.Equals(IPAddress.Loopback.MapToIPv6()))
                {
                    conn.Close();
                    return;
                }

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
                Logging.LogUsefulException(e);
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
                    Logging.LogUsefulException(e);
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
                if (bytesRead <= 0) goto Shutdown;
                foreach (IService service in _services)
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
                Logging.LogUsefulException(e);
                conn.Close();
            }
        }
    }
}
