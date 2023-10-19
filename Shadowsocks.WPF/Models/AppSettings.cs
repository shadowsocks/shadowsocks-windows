namespace Shadowsocks.WPF.Models;

public class AppSettings
{
    public bool StartOnBoot { get; set; } = false;
    public bool AssociateSsLinks { get; set; } = false;
    public bool VersionUpdateCheckForPreRelease { get; set; } = false;
    public string SkippedUpdateVersion { get; set; } = "";
}