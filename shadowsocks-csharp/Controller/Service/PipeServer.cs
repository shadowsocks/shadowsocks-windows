using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Text;

namespace Shadowsocks.Controller
{
    internal class PipeServer
    {
        public async void Run(string path)
        {
            byte[] buf = new byte[4096];
            while (true)
            {
                using (NamedPipeServerStream stream = new NamedPipeServerStream(path))
                {
                    stream.WaitForConnection();
                    await stream.ReadAsync(buf, 0, 4);
                    int strlen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));
                    await stream.ReadAsync(buf, 0, strlen);
                    string url = Encoding.UTF8.GetString(buf, 0, strlen);
                    Console.WriteLine(url);
                    stream.Close();
                }
            }
        }
    }
}
