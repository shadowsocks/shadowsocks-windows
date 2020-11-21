using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport.Header.Http
{
    public class HttpResponseObject
    {
        public string Version { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }

        public HttpResponseObject()
        {
            Version = "1.1";
            Status = "200";
            Reason = "OK";
            Headers = new()
            {
                ["Content-Type"] = new()
                {
                    "application/octet-stream",
                    "video/mpeg",
                },
                ["Transfer-Encoding"] = new()
                {
                    "chunked",
                },
                ["Connection"] = new()
                {
                    "keep-alive",
                },
                ["Pragma"] = new()
                {
                    "no-cache",
                },
                ["Cache-Control"] = new()
                {
                    "private",
                    "no-cache",
                },
            };
        }
    }
}
