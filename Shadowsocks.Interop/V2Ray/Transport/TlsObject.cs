using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class TlsObject
    {
        public string ServerName { get; set; }
        public bool AllowInsecure { get; set; }
        public List<string> Alpn { get; set; }
        public List<CertificateObject> Certificates { get; set; }
        public bool DisableSystemRoot { get; set; }

        public TlsObject()
        {
            ServerName = "";
            AllowInsecure = false;
            Alpn = new()
            {
                "h2",
                "http/1.1",
            };
            Certificates = new();
            DisableSystemRoot = false;
        }
    }
}
