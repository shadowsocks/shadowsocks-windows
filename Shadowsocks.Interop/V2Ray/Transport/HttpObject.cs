using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Transport;

public class HttpObject
{
    public List<string> Host { get; set; } = new();
    public string Path { get; set; } = "/";
}