using Shadowsocks.Interop.Settings;
using Shadowsocks.Net.Settings;
using Shadowsocks.PAC;
using System.Collections.Generic;

namespace Shadowsocks.WPF.Models;

public class Settings
{
    public AppSettings App { get; set; } = new();

    public InteropSettings Interop { get; set; } = new();

    public NetSettings Net { get; set; } = new();

    public PacSettings Pac { get; set; } = new();

    public List<Group> Groups { get; set; } = [];
}