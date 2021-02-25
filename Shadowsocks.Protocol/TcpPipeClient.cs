using Pipelines.Sockets.Unofficial;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol
{
    public class TcpPipeClient : IStreamClient
    {
        public async Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server)
        {
            var sc = await SocketConnection.ConnectAsync(destination);
            await DuplexPipe.CopyDuplexPipe(client, sc);
        }
    }
}
