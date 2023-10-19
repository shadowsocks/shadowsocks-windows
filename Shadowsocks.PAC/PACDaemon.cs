using Splat;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.PAC;

/// <summary>
/// Processing the PAC file content
/// </summary>
public class PacDaemon : IEnableLogger
{
    public const string PAC_FILE = "pac.txt";
    public const string USER_RULE_FILE = "user-rule.txt";
    public const string USER_ABP_FILE = "abp.txt";

    private FileSystemWatcher? _pacFileWatcher;
    private FileSystemWatcher? _userRuleFileWatcher;

    public event EventHandler? PACFileChanged;
    public event EventHandler? UserRuleFileChanged;

    private readonly PacSettings _PACSettings;
    private readonly GeoSiteUpdater _geoSiteUpdater;

    public PacDaemon(PacSettings pACSettings, string workingDirectory, string dlcPath)
    {
        _PACSettings = pACSettings;
        _geoSiteUpdater = new GeoSiteUpdater(dlcPath);
        TouchPacFile();
        TouchUserRuleFile();
        WatchPacFile(workingDirectory);
        WatchUserRuleFile(workingDirectory);
    }


    public string TouchPacFile()
    {
        if (!File.Exists(PAC_FILE))
        {
            _geoSiteUpdater.MergeAndWritePacFile(_PACSettings.GeoSiteDirectGroups, _PACSettings.GeoSiteProxiedGroups, _PACSettings.PacDefaultToDirect);
        }
        return PAC_FILE;
    }

    internal string TouchUserRuleFile()
    {
        if (!File.Exists(USER_RULE_FILE))
        {
            File.WriteAllText(USER_RULE_FILE, Properties.Resources.user_rule);
        }
        return USER_RULE_FILE;
    }

    internal string GetPacContent()
    {
        if (!File.Exists(PAC_FILE))
        {
            _geoSiteUpdater.MergeAndWritePacFile(_PACSettings.GeoSiteDirectGroups, _PACSettings.GeoSiteProxiedGroups, _PACSettings.PacDefaultToDirect);
        }
        return File.ReadAllText(PAC_FILE, Encoding.UTF8);
    }


    private void WatchPacFile(string workingDirectory)
    {
        _pacFileWatcher?.Dispose();
        _pacFileWatcher = new FileSystemWatcher(workingDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = PAC_FILE
        };
        _pacFileWatcher.Changed += PACFileWatcher_Changed;
        _pacFileWatcher.Created += PACFileWatcher_Changed;
        _pacFileWatcher.Deleted += PACFileWatcher_Changed;
        _pacFileWatcher.Renamed += PACFileWatcher_Changed;
        _pacFileWatcher.EnableRaisingEvents = true;
    }

    private void WatchUserRuleFile(string workingDirectory)
    {
        _userRuleFileWatcher?.Dispose();
        _userRuleFileWatcher = new FileSystemWatcher(workingDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = USER_RULE_FILE
        };
        _userRuleFileWatcher.Changed += UserRuleFileWatcher_Changed;
        _userRuleFileWatcher.Created += UserRuleFileWatcher_Changed;
        _userRuleFileWatcher.Deleted += UserRuleFileWatcher_Changed;
        _userRuleFileWatcher.Renamed += UserRuleFileWatcher_Changed;
        _userRuleFileWatcher.EnableRaisingEvents = true;
    }

    #region FileSystemWatcher.OnChanged()
    // FileSystemWatcher Changed event is raised twice
    // http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
    // Add a short delay to avoid raise event twice in a short period
    private void PACFileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (PACFileChanged != null)
        {
            this.Log().Info($"Detected: PAC file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
            Task.Factory.StartNew(() =>
            {
                ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                System.Threading.Thread.Sleep(10);
                PACFileChanged(this, EventArgs.Empty);
                ((FileSystemWatcher)sender).EnableRaisingEvents = true;
            });
        }
    }

    private void UserRuleFileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (UserRuleFileChanged != null)
        {
            this.Log().Info($"Detected: User Rule file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
            Task.Factory.StartNew(() =>
            {
                ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                System.Threading.Thread.Sleep(10);
                UserRuleFileChanged(this, EventArgs.Empty);
                ((FileSystemWatcher)sender).EnableRaisingEvents = true;
            });
        }
    }
    #endregion
}