using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using NLog;
using Shadowsocks.Localization;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Shadowsocks.Views;

namespace Shadowsocks.Controller
{
    public class UpdateChecker
    {
        private readonly Logger logger;
        private readonly HttpClient httpClient;

        // https://developer.github.com/v3/repos/releases/
        private const string UpdateURL = "https://api.github.com/repos/shadowsocks/shadowsocks-windows/releases";

        private Configuration _config;
        private Window versionUpdatePromptWindow;
        private JToken _releaseObject;

        public string NewReleaseVersion { get; private set; }
        public string NewReleaseZipFilename { get; private set; }

        public event EventHandler CheckUpdateCompleted;

        public const string Version = "4.3.3.0";
        private readonly Version _version;

        public UpdateChecker()
        {
            logger = LogManager.GetCurrentClassLogger();
            httpClient = Program.MainController.GetHttpClient();
            _version = new Version(Version);
            _config = Program.MainController.GetCurrentConfiguration();
        }

        /// <summary>
        /// Checks for updates and asks the user if updates are found.
        /// </summary>
        /// <param name="millisecondsDelay">A delay in milliseconds before checking.</param>
        /// <returns></returns>
        public async Task CheckForVersionUpdate(int millisecondsDelay = 0)
        {
            // delay
            logger.Info($"Waiting for {millisecondsDelay}ms before checking for version update.");
            await Task.Delay(millisecondsDelay);
            // update _config so we would know if the user checked or unchecked pre-release checks
            _config = Program.MainController.GetCurrentConfiguration();
            // start
            logger.Info($"Checking for version update.");
            try
            {
                // list releases via API
                var releasesListJsonString = await httpClient.GetStringAsync(UpdateURL);
                // parse
                var releasesJArray = JArray.Parse(releasesListJsonString);
                foreach (var releaseObject in releasesJArray)
                {
                    var releaseTagName = (string)releaseObject["tag_name"];
                    var releaseVersion = new Version(releaseTagName);
                    if (releaseTagName == _config.skippedUpdateVersion) // finished checking
                        break;
                    if (releaseVersion.CompareTo(_version) > 0 &&
                        (!(bool)releaseObject["prerelease"] || _config.checkPreRelease && (bool)releaseObject["prerelease"])) // selected
                    {
                        logger.Info($"Found new version {releaseTagName}.");
                        _releaseObject = releaseObject;
                        NewReleaseVersion = releaseTagName;
                        AskToUpdate(releaseObject);
                        return;
                    }
                }
                logger.Info($"No new versions found.");
                CheckUpdateCompleted?.Invoke(this, new EventArgs());
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
            }
        }

        /// <summary>
        /// Opens a window to show the update's information.
        /// </summary>
        /// <param name="releaseObject">The update release object.</param>
        private void AskToUpdate(JToken releaseObject)
        {
            if (versionUpdatePromptWindow == null)
            {
                versionUpdatePromptWindow = new Window()
                {
                    Title = LocalizationProvider.GetLocalizedValue<string>("VersionUpdate"),
                    Height = 480,
                    Width = 640,
                    MinHeight = 480,
                    MinWidth = 640,
                    Content = new VersionUpdatePromptView(releaseObject)
                };
                versionUpdatePromptWindow.Closed += VersionUpdatePromptWindow_Closed;
                versionUpdatePromptWindow.Show();
            }
            versionUpdatePromptWindow.Activate();
        }

        private void VersionUpdatePromptWindow_Closed(object sender, EventArgs e)
        {
            versionUpdatePromptWindow = null;
        }

        /// <summary>
        /// Downloads the selected update and notifies the user.
        /// </summary>
        /// <returns></returns>
        public async Task DoUpdate()
        {
            try
            {
                var assets = (JArray)_releaseObject["assets"];
                // download all assets
                foreach (JObject asset in assets)
                {
                    var filename = (string)asset["name"];
                    var browser_download_url = (string)asset["browser_download_url"];
                    var response = await httpClient.GetAsync(browser_download_url);
                    using (var downloadedFileStream = File.Create(Utils.GetTempPath(filename)))
                        await response.Content.CopyToAsync(downloadedFileStream);
                    logger.Info($"Downloaded {filename}.");
                    // store .zip filename
                    if (filename.EndsWith(".zip"))
                        NewReleaseZipFilename = filename;
                }
                logger.Info("Finished downloading.");
                // notify user
                CloseVersionUpdatePromptWindow();
                Process.Start("explorer.exe", $"/select, \"{Utils.GetTempPath(NewReleaseZipFilename)}\"");
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
            }
        }

        /// <summary>
        /// Saves the skipped update version.
        /// </summary>
        public void SkipUpdate()
        {
            var version = (string)_releaseObject["tag_name"] ?? "";
            _config.skippedUpdateVersion = version;
            Program.MainController.SaveSkippedUpdateVerion(version);
            logger.Info($"The update {version} has been skipped and will be ignored next time.");
            CloseVersionUpdatePromptWindow();
        }

        /// <summary>
        /// Closes the update prompt window.
        /// </summary>
        public void CloseVersionUpdatePromptWindow()
        {
            if (versionUpdatePromptWindow != null)
            {
                versionUpdatePromptWindow.Close();
                versionUpdatePromptWindow = null;
            }
        }
    }
}
