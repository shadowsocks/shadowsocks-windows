using System.Collections.Generic;

namespace Shadowsocks.Interop.V2Ray.Routing
{
    public class BalancerObject
    {
        /// <summary>
        /// Gets or sets the outbound tag for the load balancer.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets a list of outbound tags
        /// to include in the load balancer.
        /// </summary>
        public List<string> Selector { get; set; }

        public BalancerObject()
        {
            Tag = "";
            Selector = new();
        }
    }
}
