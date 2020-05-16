using NLog;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Controller
{

    /// <summary>
    /// Processing the PAC file content
    /// </summary>
    public class PACDaemon
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public const string PAC_FILE = "pac.txt";
        public const string USER_RULE_FILE = "user-rule.txt";
        public const string USER_ABP_FILE = "abp.txt";
        private Configuration config;

        FileSystemWatcher PACFileWatcher;
        FileSystemWatcher UserRuleFileWatcher;

        public event EventHandler PACFileChanged;
        public event EventHandler UserRuleFileChanged;

        public PACDaemon(Configuration config)
        {
            this.config = config;
            TouchPACFile();
            TouchUserRuleFile();

            this.WatchPacFile();
            this.WatchUserRuleFile();
        }


        public string TouchPACFile()
        {
            if (!File.Exists(PAC_FILE))
            {
                GeositeUpdater.MergeAndWritePACFile(config.geositeGroup, config.geositeBlacklistMode);
            }
            return PAC_FILE;
        }

        internal string TouchUserRuleFile()
        {
            if (!File.Exists(USER_RULE_FILE))
            {
                File.WriteAllText(USER_RULE_FILE, Resources.user_rule);
            }
            return USER_RULE_FILE;
        }

        internal string GetPACContent()
        {
            if (!File.Exists(PAC_FILE))
            {
                GeositeUpdater.MergeAndWritePACFile(config.geositeGroup, config.geositeBlacklistMode);
            }
            return File.ReadAllText(PAC_FILE, Encoding.UTF8);
        }


        private void WatchPacFile()
        {
            PACFileWatcher?.Dispose();
            PACFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            PACFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            PACFileWatcher.Filter = PAC_FILE;
            PACFileWatcher.Changed += PACFileWatcher_Changed;
            PACFileWatcher.Created += PACFileWatcher_Changed;
            PACFileWatcher.Deleted += PACFileWatcher_Changed;
            PACFileWatcher.Renamed += PACFileWatcher_Changed;
            PACFileWatcher.EnableRaisingEvents = true;
        }

        private void WatchUserRuleFile()
        {
            UserRuleFileWatcher?.Dispose();
            UserRuleFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            UserRuleFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            UserRuleFileWatcher.Filter = USER_RULE_FILE;
            UserRuleFileWatcher.Changed += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Created += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Deleted += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Renamed += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.EnableRaisingEvents = true;
        }

        #region FileSystemWatcher.OnChanged()
        // FileSystemWatcher Changed event is raised twice
        // http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
        // Add a short delay to avoid raise event twice in a short period
        private void PACFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (PACFileChanged != null)
            {
                logger.Info($"Detected: PAC file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
                Task.Factory.StartNew(() =>
                {
                    ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                    System.Threading.Thread.Sleep(10);
                    PACFileChanged(this, new EventArgs());
                    ((FileSystemWatcher)sender).EnableRaisingEvents = true;
                });
            }
        }

        private void UserRuleFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (UserRuleFileChanged != null)
            {
                logger.Info($"Detected: User Rule file '{e.Name}' was {e.ChangeType.ToString().ToLower()}.");
                Task.Factory.StartNew(() =>
                {
                    ((FileSystemWatcher)sender).EnableRaisingEvents = false;
                    System.Threading.Thread.Sleep(10);
                    UserRuleFileChanged(this, new EventArgs());
                    ((FileSystemWatcher)sender).EnableRaisingEvents = true;
                });
            }
        }
        #endregion
    }
}
