using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Shadowsocks.Controller
{
    public class Listener
    {
        public interface Service
        {
            bool Handle(byte[] firstPacket, int length, Socket socket, object state);
        }

        public class UDPState
        {
            public byte[] buffer = new byte[4096];
            public EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        }

        Configuration _config;
        bool _shareOverLAN;
        Socket _tcpSocket;
        Socket _udpSocket;
        IList<Service> _services;

        public Listener(IList<Service> services)
        {
            this._services = services;
        }

        private bool CheckIfPortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }
            return false;
        }

        public void Start(Configuration config)
        {
            this._config = config;
            this._shareOverLAN = config.shareOverLan;

            if (CheckIfPortInUse(_config.localPort))
                throw new Exception(I18N.GetString("Port already in use"));

            try
            {
                // Create a TCP/IP socket.
                _tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                if (_shareOverLAN)
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, _config.localPort);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Loopback, _config.localPort);
                }

                // Bind the socket to the local endpoint and listen for incoming connections.
                _tcpSocket.Bind(localEndPoint);
                _udpSocket.Bind(localEndPoint);
                _tcpSocket.Listen(1024);


                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Shadowsocks started");
                _tcpSocket.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    _tcpSocket);
                UDPState udpState = new UDPState();
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
        }

        public void RecvFromCallback(IAsyncResult ar)
        {
            UDPState state = (UDPState)ar.AsyncState;
            try
            {
                int bytesRead = _udpSocket.EndReceiveFrom(ar, ref state.remoteEndPoint);
                foreach (Service service in _services)
                {
                    if (service.Handle(state.buffer, bytesRead, _udpSocket, state))
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
            }
            finally
            {
                try
                {
                    _udpSocket.BeginReceiveFrom(state.buffer, 0, state.buffer.Length, 0, ref state.remoteEndPoint, new AsyncCallback(RecvFromCallback), state);
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
                Console.WriteLine(e);
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
                foreach (Service service in _services)
                {
                    if (service.Handle(buf, bytesRead, conn, null))
                    {
                        return;
                    }
                }
                // no service found for this
                if (conn.ProtocolType == ProtocolType.Tcp)
                {
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                conn.Close();
            }
        }
    }
}
