using Shadowsocks.Interop.V2Ray.Transport.Header;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class KcpObject
    {
        public int Mtu { get; set; }
        public int Tti { get; set; }
        public int UplinkCapacity { get; set; }
        public int DownlinkCapacity { get; set; }
        public bool Congestion { get; set; }
        public int ReadBufferSize { get; set; }
        public int WriteBufferSize { get; set; }
        public HeaderObject Header { get; set; }
        public string Seed { get; set; }

        public KcpObject()
        {
            Mtu = 1350;
            Tti = 50;
            UplinkCapacity = 5;
            DownlinkCapacity = 20;
            Congestion = false;
            ReadBufferSize = 2;
            WriteBufferSize = 2;
            Header = new();
            Seed = "";
        }
    }
}
