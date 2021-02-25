using System.IO.Pipelines;

namespace Shadowsocks.Protocol
{
    internal class PipePair
    {

        /*
         *  
         *  --> c ---w[  uplink  ]r--> s
         *  <-- c <--r[ downlink ]w--- s
         *  down   up              down
         */

        private readonly Pipe uplink = new Pipe();
        private readonly Pipe downlink = new Pipe();
        public DuplexPipe UpSide { get; private set; }
        public DuplexPipe DownSide { get; private set; }
        public PipePair()
        {
            UpSide = new DuplexPipe
            {
                Input = downlink.Reader,
                Output = uplink.Writer,
            };
            DownSide = new DuplexPipe
            {
                Input = uplink.Reader,
                Output = downlink.Writer,
            };
        }
    }
}
