using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using Newtonsoft.Json;
using NLog;
using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        [JsonIgnore]
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public string version;

        public List<Server> configs;

        public List<string> onlineConfigSource;

        // when strategy is set, index is ignored
        public string strategy;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool firstRun;
        public int localPort;
        public bool portableMode;
        public bool showPluginOutput;
        public string pacUrl;

        public bool useOnlinePac;
        public bool secureLocalPac; // enable secret for PAC server
        public bool regeneratePacOnUpdate; // regenerate pac.txt on version update
        public bool availabilityStatistics;
        public bool autoCheckUpdate;
        public bool checkPreRelease;
        public bool isVerboseLogging;

        // hidden options
        public bool isIPv6Enabled; // for experimental ipv6 support
        public bool generateLegacyUrl = false; // for pre-sip002 url compatibility
        public string geositeUrl; // for custom geosite source (and rule group)
        public string geositeGroup;
        public bool geositeBlacklistMode;
        public string userAgent;

        //public NLogConfig.LogLevel logLevel;
        public LogViewerConfig logViewer;
        public ProxyConfig proxy;
        public HotkeyConfig hotkey;

        [JsonIgnore]
        public bool firstRunOnNewVersion;

        public Configuration()
        {
            version = UpdateChecker.Version;
            strategy = "";
            index = 0;
            global = false;
            enabled = false;
            shareOverLan = false;
            firstRun = true;
            localPort = 1080;
            portableMode = true;
            showPluginOutput = false;
            pacUrl = "";
            useOnlinePac = false;
            secureLocalPac = true;
            regeneratePacOnUpdate = true;
            availabilityStatistics = false;
            autoCheckUpdate = false;
            checkPreRelease = false;
            isVerboseLogging = false;

            // hidden options
            isIPv6Enabled = false;
            generateLegacyUrl = false;
            geositeUrl = "";
            geositeGroup = "geolocation-!cn";
            geositeBlacklistMode = true;
            userAgent = "ShadowsocksWindows/$version";

            logViewer = new LogViewerConfig();
            proxy = new ProxyConfig();
            hotkey = new HotkeyConfig();

            firstRunOnNewVersion = false;

            configs = new List<Server>();
            onlineConfigSource = new List<string>();
        }

        [JsonIgnore]
        public string userAgentString; // $version substituted with numeral version in it

        [JsonIgnore]
        NLogConfig nLogConfig;

        private static readonly string CONFIG_FILE = "gui-config.json";
#if DEBUG
        private static readonly NLogConfig.LogLevel verboseLogLevel = NLogConfig.LogLevel.Trace;
#else
        private static readonly NLogConfig.LogLevel verboseLogLevel =  NLogConfig.LogLevel.Debug;
#endif

        [JsonIgnore]
        public string LocalHost => isIPv6Enabled ? "[::1]" : "127.0.0.1";

        public Server GetCurrentServer()
        {
            if (index >= 0 && index < configs.Count)
                return configs[index];
            else
                return GetDefaultServer();
        }

        public WebProxy WebProxy => enabled
            ? new WebProxy(
                    isIPv6Enabled
                    ? $"[{IPAddress.IPv6Loopback}]"
                    : IPAddress.Loopback.ToString(),
                    localPort)
            : null;

        /// <summary>
        /// Used by multiple forms to validate a server.
        /// Communication is done by throwing exceptions.
        /// </summary>
        /// <param name="server"></param>
        public static void CheckServer(Server server)
        {
            CheckServer(server.server);
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckTimeout(server.timeout, Server.MaxServerTimeoutSec);
        }

        /// <summary>
        /// Loads the configuration from file.
        /// </summary>
        /// <returns>An Configuration object.</returns>
        public static Configuration Load()
        {
            Configuration config;
            if (File.Exists(CONFIG_FILE))
            {
                try
                {
                    string configContent = File.ReadAllText(CONFIG_FILE);
                    config = JsonConvert.DeserializeObject<Configuration>(configContent);
                    return config;
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException))
                        logger.LogUsefulException(e);
                }
            }
            config = new Configuration();
            return config;
        }

        /// <summary>
        /// Process the loaded configurations and set up things.
        /// </summary>
        /// <param name="config">A reference of Configuration object.</param>
        public static void Process(ref Configuration config)
        {
            // Verify if the configured geosite group exists.
            // Reset to default if no such group.
            if (!GeositeUpdater.CheckGeositeGroup(config.geositeGroup))
            {
#if DEBUG
                logger.Debug($"Current group: {config.geositeGroup}");
                foreach (var group in GeositeUpdater.Geosites.Keys)
                    logger.Debug($"{group}");
#endif
                config.geositeGroup = "geolocation-!cn";
                logger.Warn("The specified Geosite group doesn't exist. Using default group.");
            }

            // Mark the first run of a new version.
            if (UpdateChecker.Asset.CompareVersion(UpdateChecker.Version, config.version ?? "0") > 0)
            {
                config.firstRunOnNewVersion = true;
            }
            // Add an empty server configuration
            if (config.configs.Count == 0)
                config.configs.Add(GetDefaultServer());
            // Selected server
            if (config.index == -1 && string.IsNullOrEmpty(config.strategy))
                config.index = 0;
            if (config.index >= config.configs.Count)
                config.index = config.configs.Count - 1;
            // Check OS IPv6 support
            if (!System.Net.Sockets.Socket.OSSupportsIPv6)
                config.isIPv6Enabled = false;
            config.proxy.CheckConfig();
            // Replace $version with the version number.
            config.userAgentString = config.userAgent.Replace("$version", config.version);

            // NLog log level
            try
            {
                config.nLogConfig = NLogConfig.LoadXML();
                switch (config.nLogConfig.GetLogLevel())
                {
                    case NLogConfig.LogLevel.Fatal:
                    case NLogConfig.LogLevel.Error:
                    case NLogConfig.LogLevel.Warn:
                    case NLogConfig.LogLevel.Info:
                        config.isVerboseLogging = false;
                        break;
                    case NLogConfig.LogLevel.Debug:
                    case NLogConfig.LogLevel.Trace:
                        config.isVerboseLogging = true;
                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Cannot get the log level from NLog config file. Please check if the nlog config file exists with corresponding XML nodes.\n{e.Message}");
            }
        }

        /// <summary>
        /// Saves the Configuration object to file.
        /// </summary>
        /// <param name="config">A Configuration object.</param>
        public static void Save(Configuration config)
        {
            config.configs = SortByOnlineConfig(config.configs);

            FileStream configFileStream = null;
            StreamWriter configStreamWriter = null;
            try
            {
                configFileStream = File.Open(CONFIG_FILE, FileMode.Create);
                configStreamWriter = new StreamWriter(configFileStream);
                var jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
                configStreamWriter.Write(jsonString);
                configStreamWriter.Flush();
                // NLog
                config.nLogConfig.SetLogLevel(config.isVerboseLogging ? verboseLogLevel : NLogConfig.LogLevel.Info);
                NLogConfig.SaveXML(config.nLogConfig);
            }
            catch (Exception e)
            {
                logger.LogUsefulException(e);
            }
            finally
            {
                if (configStreamWriter != null)
                    configStreamWriter.Dispose();
                if (configFileStream != null)
                    configFileStream.Dispose();
            }
        }

        public static List<Server> SortByOnlineConfig(IEnumerable<Server> servers)
        {
            var groups = servers.GroupBy(s => s.group);
            List<Server> ret = new List<Server>();
            ret.AddRange(groups.Where(g => string.IsNullOrEmpty(g.Key)).SelectMany(g => g));
            ret.AddRange(groups.Where(g => !string.IsNullOrEmpty(g.Key)).SelectMany(g => g));
            return ret;
        }

        public static Server AddDefaultServerOrServer(Configuration config, Server server = null, int? index = null)
        {
            if (config?.configs != null)
            {
                server = (server ?? GetDefaultServer());

                config.configs.Insert(index.GetValueOrDefault(config.configs.Count), server);

                //if (index.HasValue)
                //    config.configs.Insert(index.Value, server);
                //else
                //    config.configs.Add(server);
            }
            return server;
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException(I18N.GetString("Port out of range"));
        }

        public static void CheckLocalPort(int port)
        {
            CheckPort(port);
            if (port == 8123)
                throw new ArgumentException(I18N.GetString("Port can't be 8123"));
        }

        private static void CheckPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException(I18N.GetString("Password can not be blank"));
        }

        public static void CheckServer(string server)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentException(I18N.GetString("Server IP can not be blank"));
        }

        public static void CheckTimeout(int timeout, int maxTimeout)
        {
            if (timeout <= 0 || timeout > maxTimeout)
                throw new ArgumentException(
                    I18N.GetString("Timeout is invalid, it should not exceed {0}", maxTimeout));
        }

        public static void CheckProxyAuthUser(string user)
        {
            if (string.IsNullOrEmpty(user))
                throw new ArgumentException(I18N.GetString("Auth user can not be blank"));
        }

        public static void CheckProxyAuthPwd(string pwd)
        {
            if (string.IsNullOrEmpty(pwd))
                throw new ArgumentException(I18N.GetString("Auth pwd can not be blank"));
        }
    }
}
