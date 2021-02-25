using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol
{
    class DuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; set; }
        public PipeWriter Output { get; set; }

        public static Task CopyDuplexPipe(IDuplexPipe p1, IDuplexPipe p2)
        {
            var t1 = p1.Input.CopyToAsync(p2.Output);
            var t2 = p2.Input.CopyToAsync(p1.Output);

            return Task.WhenAll(t1, t2);
        }
    }
}
