using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        public bool isDefault;
        public int localPort;
        public bool portableMode;
        public bool showPluginOutput;
        public string pacUrl;

        public bool useOnlinePac;
        public bool secureLocalPac;
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
        public bool updated;

        public Configuration()
        {
            version = UpdateChecker.Version;
            strategy = "";
            index = 0;
            global = false;
            enabled = false;
            shareOverLan = false;
            isDefault = true;
            localPort = 1080;
            portableMode = true;
            showPluginOutput = false;
            pacUrl = "";
            useOnlinePac = false;
            secureLocalPac = true;
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

            updated = false;

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
        public string localHost => GetLocalHost();
        private string GetLocalHost()
        {
            return isIPv6Enabled ? "[::1]" : "127.0.0.1";
        }
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

        public static void CheckServer(Server server)
        {
            CheckServer(server.server);
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckTimeout(server.timeout, Server.MaxServerTimeoutSec);
        }

        public static bool ChecksServer(Server server)
        {
            try
            {
                CheckServer(server);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Configuration Load()
        {
            Configuration config;
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                config = JsonConvert.DeserializeObject<Configuration>(configContent);
                config.isDefault = false;
                config.version = UpdateChecker.Version;
                if (UpdateChecker.Asset.CompareVersion(UpdateChecker.Version, config.version ?? "0") > 0)
                {
                    config.updated = true;
                }

                if (config.configs.Count == 0)
                    config.configs.Add(GetDefaultServer());
                if (config.index == -1 && string.IsNullOrEmpty(config.strategy))
                    config.index = 0;
                if (!System.Net.Sockets.Socket.OSSupportsIPv6)
                {
                    config.isIPv6Enabled = false; // disable IPv6 if os not support
                }
                //TODO if remote host(server) do not support IPv6 (or DNS resolve AAAA TYPE record) disable IPv6?

                config.proxy.CheckConfig();
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    logger.LogUsefulException(e);
                config = new Configuration();
                config.configs.Add(GetDefaultServer());
            }

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
                // todo: route the error to UI since there is no log file in this scenario
                logger.Error(e, "Cannot get the log level from NLog config file. Please check if the nlog config file exists with corresponding XML nodes.");
            }

            config.userAgentString = config.userAgent.Replace("$version", config.version);

            return config;
        }

        public static void Save(Configuration config)
        {
            config.configs = SortByOnlineConfig(config.configs);
            if (config.index >= config.configs.Count)
                config.index = config.configs.Count - 1;
            if (config.index < -1)
                config.index = -1;
            if (config.index == -1 && string.IsNullOrEmpty(config.strategy))
                config.index = 0;
            config.isDefault = false;
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
                    sw.Write(jsonString);
                    sw.Flush();
                }
                try
                {
                    // apply changes to NLog.config
                    config.nLogConfig.SetLogLevel(config.isVerboseLogging ? verboseLogLevel : NLogConfig.LogLevel.Info);
                    NLogConfig.SaveXML(config.nLogConfig);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Cannot set the log level to NLog config file. Please check if the nlog config file exists with corresponding XML nodes.");
                }
            }
            catch (IOException e)
            {
                logger.LogUsefulException(e);
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
            if (password.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Password can not be blank"));
        }

        public static void CheckServer(string server)
        {
            if (server.IsNullOrEmpty())
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
            if (user.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Auth user can not be blank"));
        }

        public static void CheckProxyAuthPwd(string pwd)
        {
            if (pwd.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Auth pwd can not be blank"));
        }
    }
}
