using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport.Header.Http;

public class HttpResponseObject
{
    public string Version { get; set; } = "1.1";
    public string Status { get; set; } = "200";
    public string Reason { get; set; } = "OK";
    public Dictionary<string, List<string>> Headers { get; set; } = new()
    {
        ["Content-Type"] =
        [
            "application/octet-stream",
            "video/mpeg",
        ],
        ["Transfer-Encoding"] = ["chunked"],
        ["Connection"] = ["keep-alive"],
        ["Pragma"] = ["no-cache"],
        ["Cache-Control"] =
        [
            "private",
            "no-cache",
        ],
    };
}