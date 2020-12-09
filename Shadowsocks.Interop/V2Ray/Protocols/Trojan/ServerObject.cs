namespace Shadowsocks.Interop.V2Ray.Protocols.Trojan
{
    public class ServerObject
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public int? Level { get; set; }

        public ServerObject()
        {
            Address = "";
            Port = 0;
            Password = "";
        }

        public ServerObject(string address, int port, string password)
        {
            Address = address;
            Port = port;
            Password = password;
        }
    }
}
