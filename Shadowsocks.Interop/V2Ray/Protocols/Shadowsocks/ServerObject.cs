using System.Text.Json.Serialization;

namespace Shadowsocks.Interop.V2Ray.Protocols.Shadowsocks
{
    public class ServerObject
    {
        public string Email { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Address { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int Port { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Method { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Password { get; set; }

        public int Level { get; set; }

        public ServerObject()
        {
            Email = "";
            Address = "";
            Port = 8388;
            Method = "chacha20-ietf-poly1305";
            Password = "";
            Level = 0;
        }

        public ServerObject(string address, int port, string method, string password)
        {
            Email = "";
            Address = address;
            Port = port;
            Method = method;
            Password = password;
            Level = 0;
        }
    }
}
