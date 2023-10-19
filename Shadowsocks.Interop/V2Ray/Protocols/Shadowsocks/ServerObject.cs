namespace Shadowsocks.Interop.V2Ray.Protocols.Shadowsocks;

public class ServerObject(string address, int port, string method, string password)
{
    public string? Email { get; set; }

    public string Address { get; set; } = address;

    public int Port { get; set; } = port;

    public string Method { get; set; } = method;

    public string Password { get; set; } = password;

    public int? Level { get; set; }

    public ServerObject() : this("", 8388, "chacha20-ietf-poly1305", "") { }
}