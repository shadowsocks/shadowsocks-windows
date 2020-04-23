using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Text;

namespace Shadowsocks.Controller
{
    class RequestAddUrlEventArgs : EventArgs
    {
        public readonly string Url;

        public RequestAddUrlEventArgs(string url)
        {
            this.Url = url;
        }
    }

    internal class NamedPipeServer
    {
        public event EventHandler<RequestAddUrlEventArgs> AddUrlRequested;
        public async void Run(string path)
        {
            byte[] buf = new byte[4096];
            while (true)
            {
                using (NamedPipeServerStream stream = new NamedPipeServerStream(path))
                {
                    stream.WaitForConnection();
                    await stream.ReadAsync(buf, 0, 4);
                    int opcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));
                    if (opcode == 1)
                    {
                        await stream.ReadAsync(buf, 0, 4);
                        int strlen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

                        await stream.ReadAsync(buf, 0, strlen);
                        string url = Encoding.UTF8.GetString(buf, 0, strlen);

                        AddUrlRequested?.Invoke(this, new RequestAddUrlEventArgs(url));
                    }
                    stream.Close();
                }
            }
        }
    }
}
