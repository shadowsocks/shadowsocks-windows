using System;
using System.IO.Pipes;
using System.Net;
using System.Text;

namespace Shadowsocks.WPF.Utils
{
    class RequestAddUrlEventArgs : EventArgs
    {
        public readonly string Url;

        public RequestAddUrlEventArgs(string url)
        {
            Url = url;
        }
    }

    internal class IPCService
    {
        private const int INT32_LEN = 4;
        private const int OP_OPEN_URL = 1;
        private static readonly string PIPE_PATH = $"Shadowsocks\\{Utilities.ExecutablePath.GetHashCode()}";

        public event EventHandler<RequestAddUrlEventArgs>? OpenUrlRequested;

        public async void RunServer()
        {
            byte[] buf = new byte[4096];
            while (true)
            {
                using (NamedPipeServerStream stream = new NamedPipeServerStream(PIPE_PATH))
                {
                    await stream.WaitForConnectionAsync();
                    await stream.ReadAsync(buf, 0, INT32_LEN);
                    int opcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));
                    if (opcode == OP_OPEN_URL)
                    {
                        await stream.ReadAsync(buf, 0, INT32_LEN);
                        int strlen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

                        await stream.ReadAsync(buf, 0, strlen);
                        string url = Encoding.UTF8.GetString(buf, 0, strlen);

                        OpenUrlRequested?.Invoke(this, new RequestAddUrlEventArgs(url));
                    }
                    stream.Close();
                }
            }
        }

        private static (NamedPipeClientStream, bool) TryConnect()
        {
            NamedPipeClientStream pipe = new NamedPipeClientStream(PIPE_PATH);
            bool exist;
            try
            {
                pipe.Connect(10);
                exist = true;
            }
            catch (TimeoutException)
            {
                exist = false;
            }
            return (pipe, exist);
        }

        public static bool AnotherInstanceRunning()
        {
            (NamedPipeClientStream pipe, bool exist) = TryConnect();
            pipe.Dispose();
            return exist;
        }

        public static void RequestOpenUrl(string url)
        {
            (NamedPipeClientStream pipe, bool exist) = TryConnect();
            if(!exist) return;
            byte[] opAddUrl = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(OP_OPEN_URL));
            pipe.Write(opAddUrl, 0, INT32_LEN); // opcode addurl
            byte[] b = Encoding.UTF8.GetBytes(url);
            byte[] blen = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(b.Length));
            pipe.Write(blen, 0, INT32_LEN);
            pipe.Write(b, 0, b.Length);
            pipe.Close();
            pipe.Dispose();
        }
    }
}
