namespace Shadowsocks.Interop.V2Ray.Protocols
{
    public class AccountObject
    {
        public string User { get; set; }
        public string Pass { get; set; }

        public AccountObject()
        {
            User = "";
            Pass = "";
        }
    }
}
