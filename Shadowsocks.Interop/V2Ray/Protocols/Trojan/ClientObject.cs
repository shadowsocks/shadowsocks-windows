namespace Shadowsocks.Interop.V2Ray.Protocols.Trojan
{
    public class ClientObject
    {
        public string Password { get; set; }
        public string? Email { get; set; }
        public int? Level { get; set; }

        public ClientObject()
        {
            Password = "";
        }
    }
}
