namespace Shadowsocks.Net.Settings;

public class ForwardProxySettings
{
    public bool NoProxy { get; set; } = true;
    public bool UseSocks5Proxy { get; set; } = false;
    public bool UseHttpProxy { get; set; } = false;
    public string Address { get; set; } = "";
    public int Port { get; set; } = 1088;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}