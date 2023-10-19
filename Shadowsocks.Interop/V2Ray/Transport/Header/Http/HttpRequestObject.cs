using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport.Header.Http;

public class HttpRequestObject
{
    public string Version { get; set; } = "1.1";
    public string Method { get; set; } = "GET";
    public List<string> Path { get; set; } =
    [
        "/",
    ];

    public Dictionary<string, List<string>> Headers { get; set; } = new()
    {
        ["Host"] =
        [
            "www.baidu.com",
            "www.bing.com",
        ],
        ["User-Agent"] =
        [
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 10_0_2 like Mac OS X) AppleWebKit/601.1 (KHTML, like Gecko) CriOS/53.0.2785.109 Mobile/14A456 Safari/601.1.46",
        ],
        ["Accept-Encoding"] = ["gzip, deflate"],
        ["Connection"] = ["keep-alive"],
        ["Pragma"] = ["no-cache"],
    };
}