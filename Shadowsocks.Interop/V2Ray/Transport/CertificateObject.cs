using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class CertificateObject
    {
        public string Usage { get; set; }
        public string CertificateFile { get; set; }
        public string KeyFile { get; set; }
        public List<string> Certificate { get; set; }
        public List<string> Key { get; set; }

        public CertificateObject()
        {
            Usage = "encipherment";
            CertificateFile = "";
            KeyFile = "";
            Certificate = new();
            Key = new();
        }
    }
}
