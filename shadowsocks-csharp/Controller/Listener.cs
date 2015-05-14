using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
        Socket _socket;
        IList<Service> _services;

        private const int m_numConnections = 8000; 
        private const int m_receiveBufferSize = 4096; 
        private Semaphore m_maxNumberAcceptedClients;

        private AsyncSocketUserTokenPool m_asyncSocketUserTokenPool;
        private AsyncSocketUserTokenList m_asyncSocketUserTokenList;
        public AsyncSocketUserTokenList AsyncSocketUserTokenList { get { return m_asyncSocketUserTokenList; } }

        public Listener(IList<Service> services)
        {
            this._services = services;

            m_asyncSocketUserTokenPool = new AsyncSocketUserTokenPool(m_numConnections);
            m_asyncSocketUserTokenList = new AsyncSocketUserTokenList();
            m_maxNumberAcceptedClients = new Semaphore(m_numConnections, m_numConnections);

            AsyncSocketUserToken userToken;
            for (int i = 0; i < m_numConnections; i++)
            {
                userToken = new AsyncSocketUserToken(m_receiveBufferSize);
                userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                m_asyncSocketUserTokenPool.Push(userToken);
            }
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
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
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
                _socket.Bind(localEndPoint);
                _socket.Listen(1024);


                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Shadowsocks started");

                StartAccept(null);
            }
            catch (SocketException)
            {
                _socket.Close();
                throw;
            }
        }

        public void Stop()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }         
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArgs.AcceptSocket = null; 
            }

            m_maxNumberAcceptedClients.WaitOne(); 
            bool willRaiseEvent = _socket.AcceptAsync(acceptEventArgs);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArgs);
            }
        }


        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                ProcessAccept(acceptEventArgs);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {          
            AsyncSocketUserToken userToken = m_asyncSocketUserTokenPool.Pop();
            m_asyncSocketUserTokenList.Add(userToken);
            userToken.ConnectSocket = acceptEventArgs.AcceptSocket;

            try
            {
                bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs);  
                if (!willRaiseEvent)
                {
                    lock (userToken)
                    {
                        ProcessReceive(userToken.ReceiveEventArgs);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }

            StartAccept(acceptEventArgs);
        }


        void IO_Completed(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            AsyncSocketUserToken userToken = asyncEventArgs.UserToken as AsyncSocketUserToken;
            try
            {
                lock (userToken)
                {
                    if (asyncEventArgs.LastOperation == SocketAsyncOperation.Receive)
                        ProcessReceive(asyncEventArgs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            AsyncSocketUserToken userToken = receiveEventArgs.UserToken as AsyncSocketUserToken;
            if (userToken.ConnectSocket == null)
                return;

            if (userToken.ReceiveEventArgs.BytesTransferred > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                foreach (Service service in _services)
                {
                    if (service.Handle(userToken.ReceiveEventArgs.Buffer, userToken.ReceiveEventArgs.BytesTransferred, userToken.ConnectSocket))
                    {
                        return;
                    }
                }

                bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs);
                if (!willRaiseEvent)
                    ProcessReceive(userToken.ReceiveEventArgs);
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }

        public void CloseClientSocket(AsyncSocketUserToken userToken)
        {
            if (userToken.ConnectSocket == null)
                return;
            try
            {
                userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            userToken.ConnectSocket.Close();
            userToken.ConnectSocket = null;

            m_maxNumberAcceptedClients.Release();
            m_asyncSocketUserTokenPool.Push(userToken);
            m_asyncSocketUserTokenList.Remove(userToken);
        }
    }

}
