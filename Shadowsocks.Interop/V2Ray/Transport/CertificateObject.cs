using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class CertificateObject
    {
        public string Usage { get; set; }
        public string? CertificateFile { get; set; }
        public string? KeyFile { get; set; }
        public List<string>? Certificate { get; set; }
        public List<string>? Key { get; set; }

        public CertificateObject()
        {
            Usage = "encipherment";
        }

        public static CertificateObject DefaultFromFile => new()
        {
            CertificateFile = "",
            KeyFile = "",
        };

        public static CertificateObject DefaultEmbedded => new()
        {
            Certificate = new(),
            Key = new(),
        };
    }
}
