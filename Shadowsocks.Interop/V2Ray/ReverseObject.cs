using Shadowsocks.Interop.V2Ray.Reverse;
using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray
{
    public class ReverseObject
    {
        public List<BridgeObject> Bridges { get; set; }
        public List<PortalObject> Portals { get; set; }

        public ReverseObject()
        {
            Bridges = new();
            Portals = new();
        }
    }
}
