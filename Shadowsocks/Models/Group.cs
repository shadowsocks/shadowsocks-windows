using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Shadowsocks.Models
{
    public class Group
    {
        /// <summary>
        /// Group name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// UUID of the group.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// URL of SIP008 online configuration delivery source.
        /// </summary>
        public string OnlineConfigSource { get; set; }

        /// <summary>
        /// SIP008 configuration version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// A list of servers in the group.
        /// </summary>
        public List<Server> Servers { get; set; }

        /// <summary>
        /// Data used in bytes.
        /// The value is fetched from SIP008 provider.
        /// </summary>
        public ulong BytesUsed { get; set; }

        /// <summary>
        /// Data remaining to be used in bytes.
        /// The value is fetched from SIP008 provider.
        /// </summary>
        public ulong BytesRemaining { get; set; }

        public Group()
        {
            Name = "";
            Id = new Guid();
            OnlineConfigSource = "";
            Version = 1;
            BytesUsed = 0UL;
            BytesRemaining = 0UL;
            Servers = new List<Server>();
        }

        public Group(string name)
        {
            Name = name;
            Id = new Guid();
            OnlineConfigSource = "";
            Version = 1;
            BytesUsed = 0UL;
            BytesRemaining = 0UL;
            Servers = new List<Server>();
        }
    }
}
