using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol
{
    public interface IStreamService
    {
        Task<bool> IsMyClient(IDuplexPipe pipe);
        Task<IDuplexPipe> Handle(IDuplexPipe pipe);
    }
}
