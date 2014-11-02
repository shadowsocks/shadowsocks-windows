using shadowsocks_csharp.Properties;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace shadowsocks_csharp
{
    class PACServer
    {
        Socket listener;
        public void Start()
        {
            // Create a TCP/IP socket.
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(0, 8090);

            // Bind the socket to the local endpoint and listen for incoming connections.
            listener.Bind(localEndPoint);
            listener.Listen(100);

            listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                listener);
        }


        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);
                Socket conn = listener.EndAccept(ar);

                conn.BeginReceive(new byte[1024], 0, 256, 0,
                    new AsyncCallback(receiveCallback), conn);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void receiveCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            try
            {
                int bytesRead = conn.EndReceive(ar);

                string pac = Resources.proxy_pac;

                if (bytesRead > 0)
                {
                    string text = String.Format(@"HTTP/1.1 200 OK
Server: Shadowsocks
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: {0}
Connection: Close

", System.Text.Encoding.UTF8.GetBytes(pac).Length) + pac;
                    byte[] response = System.Text.Encoding.UTF8.GetBytes(text);
                    conn.BeginSend(response, 0, response.Length, 0, new AsyncCallback(sendCallback), conn);
                }
                else
                {
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                conn.Close();
            }
        }

        private void sendCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            conn.Shutdown(SocketShutdown.Send);
        }

    }
}
