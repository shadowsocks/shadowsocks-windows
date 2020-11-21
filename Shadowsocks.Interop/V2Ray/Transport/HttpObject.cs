using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport
{
    public class HttpObject
    {
        public List<string> Host { get; set; }
        public string Path { get; set; }

        public HttpObject()
        {
            Host = new();
            Path = "/";
        }
    }
}
