using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Protocols.VMess
{
    public class ServerObject
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public List<UserObject> Users { get; set; }

        public ServerObject()
        {
            Address = "";
            Port = 0;
            Users = new();
        }

        public ServerObject(string address, int port, string id)
        {
            Address = address;
            Port = port;
            Users = new()
            {
                new(id),
            };
        }
    }
}
