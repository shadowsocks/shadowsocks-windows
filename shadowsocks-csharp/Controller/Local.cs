using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Shadowsocks.Encrypt;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{

    class Local
    {
        private Server _server;
        private bool _shareOverLAN;
        //private Encryptor encryptor;
        Socket _listener;
        public Local(Configuration config)
        {
            this._server = config.GetCurrentServer();
            _shareOverLAN = config.shareOverLan;
            //this.encryptor = new Encryptor(config.method, config.password);
        }

        public void Start()
        {
            try
            {
                // Create a TCP/IP socket.
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                if (_shareOverLAN)
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, _server.local_port);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Loopback, _server.local_port);
                }

                // Bind the socket to the local endpoint and listen for incoming connections.
                _listener.Bind(localEndPoint);
                _listener.Listen(100);


                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Shadowsocks started");
                _listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    _listener);
            }
            catch(SocketException)
            {
                _listener.Close();
                throw;
            }

        }

        public void Stop()
        {
            _listener.Close();
        }


        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket conn = listener.EndAccept(ar);

                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                Handler handler = new Handler();
                handler.connection = conn;
                handler.encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                handler.config = _server;

                handler.Start();
            }
            catch
            {
                //Console.WriteLine(e.Message);
            }
        }

    }

    class Handler
    {
        //public Encryptor encryptor;
        public IEncryptor encryptor;
        public Server config;
        // Client  socket.
        public Socket remote;
        public Socket connection;
        // Size of receive buffer.
        public const int RecvSize = 16384;
        public const int BufferSize = RecvSize + 32;
        // remote receive buffer
        public byte[] remoteRecvBuffer = new byte[RecvSize];
        // remote send buffer
        public byte[] remoteSendBuffer = new byte[BufferSize];
        // connection receive buffer
        public byte[] connetionRecvBuffer = new byte[RecvSize];
        // connection send buffer
        public byte[] connetionSendBuffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
        private bool closed = false;

        public void Start()
        {
            try
            {
                // TODO async resolving
                IPAddress ipAddress;
                bool parsed = IPAddress.TryParse(config.server, out ipAddress);
                if (!parsed)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(config.server);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, config.server_port);


                remote = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                remote.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        public void Close()
        {
            if (closed)
            {
                return;
            }
            closed = true;
            if (connection != null)
            {
                try
                {
                    connection.Shutdown(SocketShutdown.Send);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            if (remote != null)
            {
                try
                {
                    remote.Shutdown(SocketShutdown.Send);
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            ((IDisposable)encryptor).Dispose();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the connection.
                remote.EndConnect(ar);

                //Console.WriteLine("Socket connected to {0}",
                //    remote.RemoteEndPoint.ToString());

                HandshakeReceive();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void HandshakeReceive()
        {
            try
            {
                connection.BeginReceive(connetionRecvBuffer, 0, 256, 0,
                    new AsyncCallback(HandshakeReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void HandshakeReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead > 1)
                {
                    byte[] response = { 5, 0 };
                    if (connetionRecvBuffer[0] != 5)
                    {
                        // reject socks 4
                        response = new byte[]{ 0, 91 };
                    }
                    connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(handshakeSendCallback), null);
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void handshakeSendCallback(IAsyncResult ar)
        {
            try
            {
                connection.EndSend(ar);

                // +----+-----+-------+------+----------+----------+
                // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
                // +----+-----+-------+------+----------+----------+
                // | 1  |  1  | X'00' |  1   | Variable |    2     |
                // +----+-----+-------+------+----------+----------+
                // Skip first 3 bytes
                // TODO validate
                connection.BeginReceive(connetionRecvBuffer, 0, 3, 0,
                    new AsyncCallback(handshakeReceive2Callback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void handshakeReceive2Callback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] response = { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
                    connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(startPipe), null);
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }


        private void startPipe(IAsyncResult ar)
        {
            try
            {
                connection.EndReceive(ar);
                remote.BeginReceive(remoteRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(pipeRemoteReceiveCallback), null);
                connection.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(pipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void pipeRemoteReceiveCallback(IAsyncResult ar)
        {

            try
            {
                int bytesRead = remote.EndReceive(ar);

                if (bytesRead > 0)
                {
                    int bytesToSend;
                    encryptor.Decrypt(remoteRecvBuffer, bytesRead, remoteSendBuffer, out bytesToSend);
                    connection.BeginSend(remoteSendBuffer, 0, bytesToSend, 0, new AsyncCallback(pipeConnectionSendCallback), null);
                }
                else
                {
                    Console.WriteLine("bytesRead: " + bytesRead.ToString());
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void pipeConnectionReceiveCallback(IAsyncResult ar)
        {

            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead > 0)
                {
                    int bytesToSend;
                    encryptor.Encrypt(connetionRecvBuffer, bytesRead, connetionSendBuffer, out bytesToSend);
                    remote.BeginSend(connetionSendBuffer, 0, bytesToSend, 0, new AsyncCallback(pipeRemoteSendCallback), null);
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void pipeRemoteSendCallback(IAsyncResult ar)
        {
            try
            {
                remote.EndSend(ar);
                connection.BeginReceive(this.connetionRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(pipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }

        private void pipeConnectionSendCallback(IAsyncResult ar)
        {
            try
            {
                connection.EndSend(ar);
                remote.BeginReceive(this.remoteRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(pipeRemoteReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Close();
            }
        }
    }

}
