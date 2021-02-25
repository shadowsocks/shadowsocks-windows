using Shadowsocks.Protocol.Socks5;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Shadowsocks
{
    // shadowsocks payload protocol client
    class PayloadProtocolClient : IStreamClient
    {
        public async Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server)
        {
            var addrMem = server.Output.GetMemory(512);

            var addrLen = Socks5Message.SerializeAddress(addrMem, destination);
            server.Output.Advance(addrLen);

            await DuplexPipe.CopyDuplexPipe(client, server);
        }
    }
}
