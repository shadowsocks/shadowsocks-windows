namespace Shadowsocks.Net.Settings;

public class NetSettings
{
    public bool EnableSocks5 { get; set; } = true;
    public bool EnableHttp { get; set; } = true;
    public string Socks5ListeningAddress { get; set; } = "::1";
    public string HttpListeningAddress { get; set; } = "::1";
    public int Socks5ListeningPort { get; set; } = 1080;
    public int HttpListeningPort { get; set; } = 1080;

    public ForwardProxySettings ForwardProxy { get; set; } = new();
}