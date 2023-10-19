using System.IO.Pipelines;

namespace Shadowsocks.Protocol;

internal class PipePair
{
    /*
     *
     *  --> c ---w[  uplink  ]r--> s
     *  <-- c <--r[ downlink ]w--- s
     *  down   up              down
     */

    private readonly Pipe _uplink = new();
    private readonly Pipe _downLink = new();
    public DuplexPipe UpSide { get; private set; }
    public DuplexPipe DownSide { get; private set; }
    public PipePair()
    {
        UpSide = new DuplexPipe
        {
            Input = _downLink.Reader,
            Output = _uplink.Writer,
        };
        DownSide = new DuplexPipe
        {
            Input = _uplink.Reader,
            Output = _downLink.Writer,
        };
    }
}