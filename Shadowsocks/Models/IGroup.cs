using System.Collections.Generic;

namespace Shadowsocks.Models
{
    public interface IGroup<T>
    {
        /// <summary>
        /// Gets or sets the SIP008 configuration version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the list of servers in the group.
        /// </summary>
        public List<T> Servers { get; set; }
    }
}
