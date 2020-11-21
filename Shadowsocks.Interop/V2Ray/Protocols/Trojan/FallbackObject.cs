namespace Shadowsocks.Interop.V2Ray.Protocols.Trojan
{
    public class FallbackObject
    {
        public string Alpn { get; set; }
        public string Path { get; set; }
        public object Dest { get; set; }
        public int Xver { get; set; }

        public FallbackObject()
        {
            Alpn = "";
            Path = "";
            Dest = 0;
            Xver = 0;
        }
    }
}
