using Shadowsocks.Interop.V2Ray.Transport.Header.Http;

namespace Shadowsocks.Interop.V2Ray.Transport.Header
{
    public class HttpHeaderObject : HeaderObject
    {
        public HttpRequestObject request { get; set; }

        public HttpResponseObject response { get; set; }

        public HttpHeaderObject()
        {
            Type = "http";
            request = new();
            response = new();
        }
    }
}
