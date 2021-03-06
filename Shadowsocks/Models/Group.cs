using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shadowsocks.Models
{
    public class Group : IGroup<Server>
    {
        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the UUID of the group.
        /// </summary>
        public Guid Id { get; set; }

        /// <inheritdoc/>
        public int Version { get; set; }

        /// <inheritdoc/>
        public List<Server> Servers { get; set; }

        /// <summary>
        /// Gets or sets the data usage in bytes.
        /// The value is fetched from SIP008 provider.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ulong BytesUsed { get; set; }

        /// <summary>
        /// Gets or sets the data remaining to be used in bytes.
        /// The value is fetched from SIP008 provider.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ulong BytesRemaining { get; set; }

        public Group(string name = "")
        {
            Name = name;
            Id = Guid.NewGuid();
            Version = 1;
            BytesUsed = 0UL;
            BytesRemaining = 0UL;
            Servers = new List<Server>();
        }
    }
}
