using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol.Direct
{
    public class PortForwardService : IStreamService
    {
        public async Task<IDuplexPipe> Handle(IDuplexPipe pipe) => await Task.FromResult<IDuplexPipe>(null);

        public Task<bool> IsMyClient(IDuplexPipe pipe) => Task.FromResult(true);
    }
}
