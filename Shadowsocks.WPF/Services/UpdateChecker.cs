using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using Shadowsocks.WPF.Localization;
using Shadowsocks.WPF.Models;
using Shadowsocks.WPF.ViewModels;
using Shadowsocks.WPF.Views;
using Splat;

namespace Shadowsocks.WPF.Services
{
    public class UpdateChecker : IEnableLogger
    {
        private readonly Logger logger;
        private readonly HttpClient httpClient;

        // https://developer.github.com/v3/repos/releases/
        private const string UpdateURL = "https://api.github.com/repos/shadowsocks/shadowsocks-windows/releases";

        private Window? versionUpdatePromptWindow;
        private JsonElement _releaseObject;

        public string NewReleaseVersion { get; private set; }
        public string NewReleaseZipFilename { get; private set; }

        public event EventHandler? CheckUpdateCompleted;

        public static readonly string Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "5.0.0";
        private readonly Version _version;

        public UpdateChecker()
        {
            logger = LogManager.GetCurrentClassLogger();
            httpClient = Locator.Current.GetService<HttpClient>();
            _version = new Version(Version);
            NewReleaseVersion = "";
            NewReleaseZipFilename = "";
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
            // start
            logger.Info($"Checking for version update.");
            var appSettings = Locator.Current.GetService<AppSettings>();
            try
            {
                // list releases via API
                var releasesListJsonStream = await httpClient.GetStreamAsync(UpdateURL);
                // parse
                using (JsonDocument jsonDocument = await JsonDocument.ParseAsync(releasesListJsonStream))
                {
                    var releasesList = jsonDocument.RootElement;
                    foreach (var releaseObject in releasesList.EnumerateArray())
                    {
                        var releaseTagName = releaseObject.GetProperty("tag_name").GetString();
                        var releaseVersion = new Version(releaseTagName ?? "5.0.0");
                        var releaseIsPrerelease = releaseObject.GetProperty("prerelease").GetBoolean();
                        if (releaseTagName == appSettings.SkippedUpdateVersion) // finished checking
                            break;
                        if (releaseVersion.CompareTo(_version) > 0 &&
                            (!releaseIsPrerelease || appSettings.VersionUpdateCheckForPrerelease && releaseIsPrerelease)) // selected
                        {
                            logger.Info($"Found new version {releaseTagName}.");
                            _releaseObject = releaseObject;
                            NewReleaseVersion = releaseTagName ?? "";
                            AskToUpdate(releaseObject);
                            return;
                        }
                    }
                }
                logger.Info($"No new versions found.");
                CheckUpdateCompleted?.Invoke(this, new EventArgs());
            }
            catch (Exception e)
            {
                this.Log().Error(e, "An error occurred while checking for version updates.");
            }
        }

        /// <summary>
        /// Opens a window to show the update's information.
        /// </summary>
        /// <param name="releaseObject">The update release object.</param>
        private void AskToUpdate(JsonElement releaseObject)
        {
            if (versionUpdatePromptWindow == null)
            {
                var versionUpdatePromptView = new VersionUpdatePromptView()
                {
                    ViewModel = new VersionUpdatePromptViewModel(releaseObject),
                };
                versionUpdatePromptWindow = new Window()
                {
                    Title = LocalizationProvider.GetLocalizedValue<string>("VersionUpdate"),
                    Height = 480,
                    Width = 640,
                    MinHeight = 480,
                    MinWidth = 640,
                    Content = versionUpdatePromptView,
                };
                versionUpdatePromptWindow.Closed += VersionUpdatePromptWindow_Closed;
                versionUpdatePromptWindow.Show();
            }
            versionUpdatePromptWindow.Activate();
        }

        private void VersionUpdatePromptWindow_Closed(object? sender, EventArgs e)
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
                var assets = _releaseObject.GetProperty("assets");
                // download all assets
                foreach (var asset in assets.EnumerateArray())
                {
                    var filename = asset.GetProperty("name").GetString();
                    var browser_download_url = asset.GetProperty("browser_download_url").GetString();
                    var response = await httpClient.GetAsync(browser_download_url);
                    if (filename is string)
                    {
                        using (var downloadedFileStream = File.Create(Utils.Utilities.GetTempPath(filename)))
                            await response.Content.CopyToAsync(downloadedFileStream);
                        logger.Info($"Downloaded {filename}.");
                        // store .zip filename
                        if (filename.EndsWith(".zip"))
                            NewReleaseZipFilename = filename;
                    }
                }
                logger.Info("Finished downloading.");
                // notify user
                CloseVersionUpdatePromptWindow();
                Process.Start("explorer.exe", $"/select, \"{Utils.Utilities.GetTempPath(NewReleaseZipFilename)}\"");
            }
            catch (Exception e)
            {
                this.Log().Error(e, "An error occurred while downloading the version update.");
            }
        }

        /// <summary>
        /// Saves the skipped update version.
        /// </summary>
        public void SkipUpdate()
        {
            var appSettings = Locator.Current.GetService<AppSettings>();
            if (_releaseObject.TryGetProperty("tag_name", out var tagNameJsonElement) && tagNameJsonElement.GetString() is string version)
            {
                appSettings.SkippedUpdateVersion = version;
                // TODO: signal settings change
                logger.Info($"The update {version} has been skipped and will be ignored next time.");
            }
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
