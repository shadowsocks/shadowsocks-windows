using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        public string version;

        public List<Server> configs;

        // when strategy is set, index is ignored
        public string strategy;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public bool isIPv6Enabled = false;
        public int localPort;
        public bool portableMode = true;
        public string pacUrl;
        public bool useOnlinePac;
        public bool secureLocalPac = true;
        public bool availabilityStatistics;
        public bool autoCheckUpdate;
        public bool checkPreRelease;
        public bool isVerboseLogging;
        public LogViewerConfig logViewer;
        public ProxyConfig proxy;
        public HotkeyConfig hotkey;

        private static readonly string CONFIG_FILE = "gui-config.json";
        [JsonIgnore]
        public string localHost => GetLocalHost();
        private string GetLocalHost() {
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
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                Configuration config = JsonConvert.DeserializeObject<Configuration>(configContent);
                config.isDefault = false;

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
                if (!System.Net.Sockets.Socket.OSSupportsIPv6) {
                    config.isIPv6Enabled = false; // disable IPv6 if os not support
                }
                //TODO if remote host(server) do not support IPv6 (or DNS resolve AAAA TYPE record) disable IPv6?

                config.proxy.CheckConfig();

                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Logging.LogUsefulException(e);
                return new Configuration
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
                    hotkey = new HotkeyConfig()
                };
            }
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
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public static Server AddDefaultServerOrServer(Configuration config, Server server = null, int? index = null)
        {
            if (config != null && config.configs != null)
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

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception(I18N.GetString("assertion failure"));
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
