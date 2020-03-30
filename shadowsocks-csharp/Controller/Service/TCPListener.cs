using NLog;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Shadowsocks.Controller
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

    public class TCPListener
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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
        IEnumerable<IStreamService> _services;

        public TCPListener(Configuration config, IEnumerable<IStreamService> services)
        {
            _config = config;
            _shareOverLAN = config.shareOverLan;
            _services = services;
        }

        private bool CheckIfPortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            return ipProperties.GetActiveTcpListeners().Any(endPoint => endPoint.Port == port);
        }

        public void Start()
        {
            if (CheckIfPortInUse(_config.localPort))
            {
                throw new Exception(I18N.GetString("Port {0} already in use", this._config.localPort));
            }

            try
            {
                // Create a TCP/IP socket.
                _tcpSocket = new Socket(_config.isIPv6Enabled ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                localEndPoint = _shareOverLAN
                    ? new IPEndPoint(_config.isIPv6Enabled ? IPAddress.IPv6Any : IPAddress.Any, this._config.localPort)
                    : new IPEndPoint(_config.isIPv6Enabled ? IPAddress.IPv6Loopback : IPAddress.Loopback, this._config.localPort);

                // Bind the socket to the local endpoint and listen for incoming connections.
                _tcpSocket.Bind(localEndPoint);
                _tcpSocket.Listen(1024);

                // Start an asynchronous socket to listen for connections.
                logger.Info($"Shadowsocks started TCP ({UpdateChecker.Version})");
                logger.Debug(Encryption.EncryptorFactory.DumpRegisteredEncryptor());
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
                logger.LogUsefulException(e);
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
                    logger.LogUsefulException(e);
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
                logger.LogUsefulException(e);
                conn.Close();
            }
        }
    }
}
