using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace shadowsocks_csharp
{


    class Local
    {
        private Config config;
        private Encryptor encryptor;
        Socket listener;
        public Local(Config config)
        {
            this.config = config;
            this.encryptor = new Encryptor(config.method, config.password);
        }

        public void Start()
        {

            // Create a TCP/IP socket.
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(0, config.local_port);

            // Bind the socket to the local endpoint and listen for incoming connections.
            listener.Bind(localEndPoint);
            listener.Listen(100);


            // Start an asynchronous socket to listen for connections.
            Console.WriteLine("Waiting for a connection...");
            listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                listener);

        }

        public void Stop()
        {
            listener.Close();
        }


        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {

                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                Socket conn = listener.EndAccept(ar);

                // Create the state object.
                Handler handler = new Handler();
                handler.connection = conn;
                if (encryptor.method == "table") {
                    handler.encryptor = encryptor;
                } else {
                    handler.encryptor = new Encryptor(config.method, config.password);
                }
                handler.config = config;

                handler.Start();
                //handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                //    new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }

    class Handler
    {
        public Encryptor encryptor;
        public Config config;
        // Client  socket.
        public Socket remote;
        public Socket connection;
        // Size of receive buffer.
        public const int BufferSize = 1500;
        // remote receive buffer
        public byte[] remoteBuffer = new byte[BufferSize];
        // connection receive buffer
        public byte[] connetionBuffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();

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
                    new AsyncCallback(connectCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        public void Close()
        {
            connection.Shutdown(SocketShutdown.Send);
            if (remote != null)
            {
                remote.Shutdown(SocketShutdown.Send);
            }
            encryptor.Dispose();
        }

        private void connectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the connection.
                remote.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    remote.RemoteEndPoint.ToString());

                handshakeReceive();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        private void handshakeReceive()
        {
            try
            {
                connection.BeginReceive(new byte[256], 0, 256, 0,
                    new AsyncCallback(handshakeReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        private void handshakeReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = connection.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] response = { 5, 0 };
                    connection.BeginSend(response, 0, response.Length, 0, new AsyncCallback(handshakeSendCallback), null);
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
                connection.BeginReceive(new byte[3], 0, 3, 0,
                    new AsyncCallback(handshakeReceive2Callback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }


        private void startPipe(IAsyncResult ar)
        {
            try
            {
                connection.EndReceive(ar);
                remote.BeginReceive(remoteBuffer, 0, BufferSize, 0,
                    new AsyncCallback(pipeRemoteReceiveCallback), null);
                connection.BeginReceive(connetionBuffer, 0, BufferSize, 0,
                    new AsyncCallback(pipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
                    byte[] buf = encryptor.Decrypt(remoteBuffer, bytesRead);
                    connection.BeginSend(buf, 0, buf.Length, 0, new AsyncCallback(pipeConnectionSendCallback), null);
                }
                else
                {
                    Console.WriteLine("bytesRead: " + bytesRead.ToString());
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
                    byte[] buf = encryptor.Encrypt(connetionBuffer, bytesRead);
                    remote.BeginSend(buf, 0, buf.Length, 0, new AsyncCallback(pipeRemoteSendCallback), null);
                }
                else
                {
                    Console.WriteLine("bytesRead: " + bytesRead.ToString());
                    this.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        private void pipeRemoteSendCallback(IAsyncResult ar)
        {
            try
            {
                remote.EndSend(ar);
                connection.BeginReceive(this.connetionBuffer, 0, BufferSize, 0,
                    new AsyncCallback(pipeConnectionReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }

        private void pipeConnectionSendCallback(IAsyncResult ar)
        {
            try
            {
                connection.EndSend(ar);
                remote.BeginReceive(this.remoteBuffer, 0, BufferSize, 0,
                    new AsyncCallback(pipeRemoteReceiveCallback), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                this.Close();
            }
        }
    }

}
