using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shadowsocks.Models;

public class Group(string name = "") : IGroup<Server>
{
    /// <summary>
    /// Gets or sets the UUID of the group.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string Name { get; set; } = name;

    /// <inheritdoc/>
    public int Version { get; set; } = 1;

    /// <inheritdoc/>
    public List<Server> Servers { get; set; } = [];

    /// <summary>
    /// Gets or sets the data usage in bytes.
    /// The value is fetched from SIP008 provider.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ulong BytesUsed { get; set; } = 0UL;

    /// <summary>
    /// Gets or sets the data remaining to be used in bytes.
    /// The value is fetched from SIP008 provider.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ulong BytesRemaining { get; set; } = 0UL;
}