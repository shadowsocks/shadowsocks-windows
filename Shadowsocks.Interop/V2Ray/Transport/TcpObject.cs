using Shadowsocks.Interop.V2Ray.Transport.Header;

namespace Shadowsocks.Interop.V2Ray.Transport;

public class TcpObject
{
    /// <summary>
    /// Gets or sets whether to use PROXY protocol.
    /// </summary>
    public bool AcceptProxyProtocol { get; set; } = false;

    /// <summary>
    /// Gets or sets the header options.
    /// </summary>
    public object Header { get; set; } = new HeaderObject();

    public static TcpObject DefaultHttp => new()
    {
        Header = new HttpHeaderObject(),
    };
}