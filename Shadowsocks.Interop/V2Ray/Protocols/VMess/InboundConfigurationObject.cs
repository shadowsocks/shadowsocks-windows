using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Protocols.VMess
{
    public class InboundConfigurationObject
    {
        public List<UserObject> Clients { get; set; }
        public UserObject? Default { get; set; }
        public DetourObject? Detour { get; set; }

        public InboundConfigurationObject()
        {
            Clients = new();
        }
    }
}
