using System.Collections.Generic;
using System.Net;

namespace Shadowsocks.Interop.V2Ray.Protocols.Socks
{
    public class ServerObject
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public List<UserObject>? Users { get; set; }

        public ServerObject()
        {
            Address = "";
            Port = 0;
        }

        public ServerObject(DnsEndPoint socksEndPoint, string? username = null, string? password = null)
        {
            Address = socksEndPoint.Host;
            Port = socksEndPoint.Port;
            Users = new();
            var hasCredential = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
            if (hasCredential)
            {
                var user = new UserObject()
                {
                    User = username!, // null check already performed at line 23.
                    Pass = password!,
                };
                Users = new()
                {
                    user,
                };
            }
        }
    }
}
