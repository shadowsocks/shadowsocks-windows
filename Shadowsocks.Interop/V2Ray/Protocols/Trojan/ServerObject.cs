namespace Shadowsocks.Interop.V2Ray.Protocols.Trojan;

public class ServerObject(string address, int port, string password)
{
    public string Address { get; set; } = address;
    public int Port { get; set; } = port;
    public string Password { get; set; } = password;
    public string? Email { get; set; }
    public int? Level { get; set; }

    public ServerObject() : this("", 0, "") { }
}