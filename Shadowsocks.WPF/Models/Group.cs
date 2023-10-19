namespace Shadowsocks.WPF.Models;

public class Group(string name) : Shadowsocks.Models.Group(name)
{
    /// <summary>
    /// Gets or sets the URL of SIP008 online configuration delivery source.
    /// </summary>
    public string OnlineConfigSource { get; set; } = string.Empty;

    public Group() : this(string.Empty) { }
}