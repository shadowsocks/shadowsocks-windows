namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class SockoptObject
    {
        public int Mark { get; set; }
        public bool TcpFastOpen { get; set; }
        public string Tproxy { get; set; }

        public SockoptObject()
        {
            Mark = 0;
            TcpFastOpen = false;
            Tproxy = "off";
        }
    }
}
