using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.V2Ray.Protocols.Shadowsocks
{
    public class ServerObject
    {
        public string? Email { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public string Method { get; set; }

        public string Password { get; set; }

        public int? Level { get; set; }

        public ServerObject()
        {
            Address = "";
            Port = 8388;
            Method = "chacha20-ietf-poly1305";
            Password = "";
        }

        public ServerObject(string address, int port, string method, string password)
        {
            Address = address;
            Port = port;
            Method = method;
            Password = password;
        }
    }
}
