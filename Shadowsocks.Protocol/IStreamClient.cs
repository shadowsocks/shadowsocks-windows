using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol
{
    interface IStreamClient
    {
        Task Connect(EndPoint destination, IDuplexPipe client, IDuplexPipe server);
    }
}