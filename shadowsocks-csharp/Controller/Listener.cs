using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Shadowsocks.Controller
{
    public class Listener
    {
        public interface Service
        {
            bool Handle(byte[] firstPacket, int length, Socket socket);
        }

        Configuration _config;
        bool _shareOverLAN;
        bool _bypassWhiteList;
        string _authUser;
        string _authPass;
        Socket _socket;
        Socket _socket_v6;
        bool _stop;
        IList<Service> _services;
        protected System.Timers.Timer timer;
        protected object timerLock = new object();

        public Listener(IList<Service> services)
        {
            this._services = services;
            _stop = false;
        }

        public IList<Service> GetServices()
        {
            return _services;
        }

        private bool CheckIfPortInUse(int port)
        {
            try
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
            }
            catch
            {

            }
            return false;
        }

        public bool isConfigChange(Configuration config)
        {
            try
            {
                if (this._shareOverLAN != config.shareOverLan
                    || _authUser != config.authUser
                    || _authPass != config.authPass
                    || _bypassWhiteList != config.bypassWhiteList
                    || _socket == null
                    || ((IPEndPoint)_socket.LocalEndPoint).Port != config.localPort)
                {
                    return true;
                }
            }
            catch (Exception)
            { }
            return false;
        }

        public void Start(Configuration config, int port)
        {
            this._config = config;
            this._shareOverLAN = config.shareOverLan;
            this._authUser = config.authUser;
            this._authPass = config.authPass;
            this._bypassWhiteList = config.bypassWhiteList;
            _stop = false;

            int localPort = port == 0 ? _config.localPort : port;
            if (CheckIfPortInUse(localPort))
                throw new Exception(I18N.GetString("Port already in use"));

            try
            {
                // Create a TCP/IP socket.
                bool ipv6 = true;
                //bool ipv6 = false;
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                if (ipv6)
                {
                    try
                    {
                        _socket_v6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                        //_socket_v6.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                        _socket_v6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    }
                    catch
                    {
                        _socket_v6 = null;
                    }
                }
                IPEndPoint localEndPoint = null;
                IPEndPoint localEndPointV6 = null;
                if (_shareOverLAN)
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
                    localEndPointV6 = new IPEndPoint(IPAddress.IPv6Any, localPort);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Loopback, localPort);
                    localEndPointV6 = new IPEndPoint(IPAddress.IPv6Loopback, localPort);
                }

                // Bind the socket to the local endpoint and listen for incoming connections.
                if (_socket_v6 != null)
                {
                    _socket_v6.Bind(localEndPointV6);
                    _socket_v6.Listen(1024);
                }
                try
                {
                    //throw new SocketException();
                    _socket.Bind(localEndPoint);
                    _socket.Listen(1024);
                }
                catch (SocketException e)
                {
                    if (_socket_v6 == null)
                    {
                        throw e;
                    }
                    else
                    {
                        _socket.Close();
                        _socket = _socket_v6;
                        _socket_v6 = null;
                    }
                }

                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("ShadowsocksR started on port " + localPort.ToString());
                _socket.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    _socket);
                if (_socket_v6 != null)
                    _socket_v6.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        _socket_v6);
            }
            catch (SocketException)
            {
                _socket.Close();
                if (_socket_v6 != null)
                    _socket_v6.Close();
                throw;
            }
        }

        public void Stop()
        {
            ResetTimeout(0, null);
            _stop = true;
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
            if (_socket_v6 != null)
            {
                _socket_v6.Close();
                _socket_v6 = null;
            }
        }

        private void ResetTimeout(Double time, Socket socket)
        {
            if (time <= 0 && timer == null)
                return;

            lock (timerLock)
            {
                if (time <= 0)
                {
                    if (timer != null)
                    {
                        timer.Enabled = false;
                        timer.Elapsed -= (sender, e) => timer_Elapsed(sender, e, socket);
                        timer.Dispose();
                        timer = null;
                    }
                }
                else
                {
                    if (timer == null)
                    {
                        timer = new System.Timers.Timer(time * 1000.0);
                        timer.Elapsed += (sender, e) => timer_Elapsed(sender, e, socket);
                        timer.Start();
                    }
                    else
                    {
                        timer.Interval = time * 1000.0;
                        timer.Stop();
                        timer.Start();
                    }
                }
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs eventArgs, Socket socket)
        {
            if (timer == null)
            {
                return;
            }
            Socket listener = socket;
            try
            {
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);
                ResetTimeout(0, listener);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                ResetTimeout(5, listener);
            }
        }


        public void AcceptCallback(IAsyncResult ar)
        {
            if (_stop) return;

            Socket listener = (Socket)ar.AsyncState;
            try
            {
                Socket conn = listener.EndAccept(ar);

                if ((_authUser ?? "").Length == 0 && !Util.Utils.isLAN(conn))
                {
                    conn.Shutdown(SocketShutdown.Both);
                    conn.Close();
                }
                else
                {
                    byte[] buf = new byte[4096];
                    object[] state = new object[] {
                        conn,
                        buf
                    };

                    int local_port = ((IPEndPoint)conn.LocalEndPoint).Port;
                    if (!_config.GetPortMapCache().ContainsKey(local_port) || _config.GetPortMapCache()[local_port].type != 0)
                    {
                        conn.BeginReceive(buf, 0, buf.Length, 0,
                            new AsyncCallback(ReceiveCallback), state);
                    }
                    else
                    {
                        foreach (Service service in _services)
                        {
                            if (service.Handle(buf, 0, conn))
                            {
                                return;
                            }
                        }
                        // no service found for this
                        // shouldn't happen
                        conn.Shutdown(SocketShutdown.Both);
                        conn.Close();
                    }
                }
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
                    ResetTimeout(5, listener);
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
                    if (service.Handle(buf, bytesRead, conn))
                    {
                        return;
                    }
                }
                // no service found for this
                // shouldn't happen
                conn.Shutdown(SocketShutdown.Both);
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                conn.Shutdown(SocketShutdown.Both);
                conn.Close();
            }
        }
    }
}
