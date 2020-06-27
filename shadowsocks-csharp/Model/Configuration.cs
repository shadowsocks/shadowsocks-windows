using System;
using System.Collections.Generic;
using System.IO;
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

        // when strategy is set, index is ignored
        public string strategy;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public int localPort;
        public bool portableMode = true;
        public bool showPluginOutput;
        public string pacUrl;

        public bool useOnlinePac;
        public bool secureLocalPac = true;
        public bool availabilityStatistics;
        public bool autoCheckUpdate;
        public bool checkPreRelease;
        public bool isVerboseLogging;

        // hidden options
        public bool isIPv6Enabled = false; // for experimental ipv6 support
        public bool generateLegacyUrl = false; // for pre-sip002 url compatibility
        public string geositeUrl; // for custom geosite source (and rule group)
        public string geositeGroup = "geolocation-!cn";
        public bool geositeBlacklistMode = true;


        //public NLogConfig.LogLevel logLevel;
        public LogViewerConfig logViewer;
        public ProxyConfig proxy;
        public HotkeyConfig hotkey;

        [JsonIgnore]
        NLogConfig nLogConfig;

        private static readonly string CONFIG_FILE = "gui-config.json";
#if DEBUG
        private static readonly NLogConfig.LogLevel verboseLogLevel = NLogConfig.LogLevel.Trace;
#else
        private static readonly NLogConfig.LogLevel verboseLogLevel =  NLogConfig.LogLevel.Debug;
#endif


        [JsonIgnore]
        public bool updated = false;

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
                if (UpdateChecker.Asset.CompareVersion(UpdateChecker.Version, config.version ?? "0") > 0)
                {
                    config.updated = true;
                }

                if (config.configs == null)
                    config.configs = new List<Server>();
                if (config.configs.Count == 0)
                    config.configs.Add(GetDefaultServer());
                if (config.localPort == 0)
                    config.localPort = 1080;
                if (config.index == -1 && config.strategy == null)
                    config.index = 0;
                if (config.logViewer == null)
                    config.logViewer = new LogViewerConfig();
                if (config.proxy == null)
                    config.proxy = new ProxyConfig();
                if (config.hotkey == null)
                    config.hotkey = new HotkeyConfig();
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
                config = new Configuration
                {
                    index = 0,
                    isDefault = true,
                    localPort = 1080,
                    autoCheckUpdate = true,
                    configs = new List<Server>()
                    {
                        GetDefaultServer()
                    },
                    logViewer = new LogViewerConfig(),
                    proxy = new ProxyConfig(),
                    hotkey = new HotkeyConfig(),
                };
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

            return config;
        }

        public static void Save(Configuration config)
        {
            config.version = UpdateChecker.Version;
            if (config.index >= config.configs.Count)
                config.index = config.configs.Count - 1;
            if (config.index < -1)
                config.index = -1;
            if (config.index == -1 && config.strategy == null)
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
