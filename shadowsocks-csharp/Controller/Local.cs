using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Shadowsocks.Encryption;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{

    class Local
    {
        private Configuration _config;
        private bool _shareOverLAN;
        //private Encryptor encryptor;
        Socket _listener;
        public Local(Configuration config)
        {
            this._config = config;
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
                    localEndPoint = new IPEndPoint(IPAddress.Any, _config.localPort);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Loopback, _config.localPort);
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
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                Socket conn = listener.EndAccept(ar);
                conn.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

                Handler handler = new Handler();
                handler.connection = conn;
                Server server = _config.GetCurrentServer();
                handler.encryptor = EncryptorFactory.GetEncryptor(server.method, server.password);
                handler.server = server;

                handler.Start();
            }
            catch
            {
                //Console.WriteLine(e.Message);
            }
            finally
            {
                try
                {
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
                }
                catch
                {
                    //Console.WriteLine(e.Message);
                }
            }
        }

    }

    class Handler
    {
        //public Encryptor encryptor;
        public IEncryptor encryptor;
        public Server server;
        // Client  socket.
        public Socket remote;
        public Socket connection;
        // Size of receive buffer.
        public const int RecvSize = 16384;
        public const int BufferSize = RecvSize + 32;
        // remote receive buffer
        private byte[] remoteRecvBuffer = new byte[RecvSize];
        // remote send buffer
        private byte[] remoteSendBuffer = new byte[BufferSize];
        // connection receive buffer
        private byte[] connetionRecvBuffer = new byte[RecvSize];
        // connection send buffer
        private byte[] connetionSendBuffer = new byte[BufferSize];
        // Received data string.

        private bool connectionShutdown = false;
        private bool remoteShutdown = false;
        private bool closed = false;
        
        private object encryptionLock = new object();
        private object decryptionLock = new object();

        public void Start()
        {
            try
            {
                // TODO async resolving
                IPAddress ipAddress;
                bool parsed = IPAddress.TryParse(server.server, out ipAddress);
                if (!parsed)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(server.server);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, server.server_port);


                remote = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

                // Connect to the remote endpoint.
                remote.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void CheckClose()
        {
            if (connectionShutdown && remoteShutdown)
            {
                this.Close();
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (closed)
                {
                    return;
                }
                closed = true;
            }
            if (connection != null)
            {
                try
                {
                    connection.Shutdown(SocketShutdown.Both);
                    connection.Close();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                }
            }
            if (remote != null)
            {
                try
                {
                    remote.Shutdown(SocketShutdown.Both);
                    remote.Close();
                }
                catch (SocketException e)
                {
                    Logging.LogUsefulException(e);
                }
            }
            lock (encryptionLock)
            {
                lock (decryptionLock)
                {
                    ((IDisposable)encryptor).Dispose();
                }
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
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
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void HandshakeReceive()
        {
            if (closed)
            {
                return;
            }
            try
            {
                connection.BeginReceive(connetionRecvBuffer, 0, 256, 0,
                    new AsyncCallback(HandshakeReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void HandshakeReceiveCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
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
                        Console.WriteLine("socks 5 protocol error");
                    }
                    connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(HandshakeSendCallback), null);
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void HandshakeSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
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
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void handshakeReceive2Callback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] response = { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
                    connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(StartPipe), null);
                }
                else
                {
                    Console.WriteLine("failed to recv data in handshakeReceive2Callback");
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }


        private void StartPipe(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connection.EndReceive(ar);
                remote.BeginReceive(remoteRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(PipeRemoteReceiveCallback), null);
                connection.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(PipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void PipeRemoteReceiveCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                int bytesRead = remote.EndReceive(ar);

                if (bytesRead > 0)
                {
                    int bytesToSend;
                    lock (decryptionLock)
                    {
                        if (closed)
                        {
                            return;
                        }
                        encryptor.Decrypt(remoteRecvBuffer, bytesRead, remoteSendBuffer, out bytesToSend);
                    }
                    connection.BeginSend(remoteSendBuffer, 0, bytesToSend, 0, new AsyncCallback(PipeConnectionSendCallback), null);
                }
                else
                {
                    //Console.WriteLine("bytesRead: " + bytesRead.ToString());
                    connection.Shutdown(SocketShutdown.Send);
                    connectionShutdown = true;
                    CheckClose();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void PipeConnectionReceiveCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead > 0)
                {
                    int bytesToSend;
                    lock (encryptionLock)
                    {
                        if (closed)
                        {
                            return;
                        }
                        encryptor.Encrypt(connetionRecvBuffer, bytesRead, connetionSendBuffer, out bytesToSend);
                    }
                    remote.BeginSend(connetionSendBuffer, 0, bytesToSend, 0, new AsyncCallback(PipeRemoteSendCallback), null);
                }
                else
                {
                    remote.Shutdown(SocketShutdown.Send);
                    remoteShutdown = true;
                    CheckClose();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void PipeRemoteSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                remote.EndSend(ar);
                connection.BeginReceive(this.connetionRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(PipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }

        private void PipeConnectionSendCallback(IAsyncResult ar)
        {
            if (closed)
            {
                return;
            }
            try
            {
                connection.EndSend(ar);
                remote.BeginReceive(this.remoteRecvBuffer, 0, RecvSize, 0,
                    new AsyncCallback(PipeRemoteReceiveCallback), null);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                this.Close();
            }
        }
    }

}
