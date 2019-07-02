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
        bool _shareOverLAN;
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
            this._shareOverLAN = config.shareOverLan;

            if (CheckIfPortInUse(_config.localPort))
                throw new Exception(I18N.GetString("Port {0} already in use", _config.localPort));

            try
            {
                // Create a TCP/IP socket.
                _tcpSocket = new Socket(config.isIPv6Enabled ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _udpSocket = new Socket(config.isIPv6Enabled ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                localEndPoint = _shareOverLAN
                    ? new IPEndPoint(config.isIPv6Enabled ? IPAddress.IPv6Any : IPAddress.Any, _config.localPort)
                    : new IPEndPoint(config.isIPv6Enabled ? IPAddress.IPv6Loopback : IPAddress.Loopback, _config.localPort);

                // Bind the socket to the local endpoint and listen for incoming connections.
                _tcpSocket.Bind(localEndPoint);
                _udpSocket.Bind(localEndPoint);
                _tcpSocket.Listen(1024);

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
