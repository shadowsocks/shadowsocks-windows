using Shadowsocks.Interop.V2Ray.Transport.Header.Http;

namespace Shadowsocks.Interop.V2Ray.Transport.Header;

public class HttpHeaderObject : HeaderObject
{
    public HttpRequestObject Request { get; set; }

    public HttpResponseObject Response { get; set; }

    public HttpHeaderObject()
    {
        Type = "http";
        Request = new();
        Response = new();
    }
}