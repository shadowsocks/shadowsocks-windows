using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport;

public class CertificateObject
{
    public string Usage { get; set; } = "encipherment";
    public string? CertificateFile { get; set; }
    public string? KeyFile { get; set; }
    public List<string>? Certificate { get; set; }
    public List<string>? Key { get; set; }

    public static CertificateObject DefaultFromFile => new()
    {
        CertificateFile = string.Empty,
        KeyFile = string.Empty,
    };

    public static CertificateObject DefaultEmbedded => new()
    {
        Certificate = [],
        Key = [],
    };
}