using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class TlsObject
    {
        public string? ServerName { get; set; }
        public bool AllowInsecure { get; set; }
        public List<string>? Alpn { get; set; }
        public List<CertificateObject>? Certificates { get; set; }
        public bool DisableSystemRoot { get; set; }
    }
}
