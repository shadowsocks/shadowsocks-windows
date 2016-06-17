using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Models
{
    [Serializable]
    public class OfficialConfig
    {
        public List<Server> configs;

        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public int localPort;
        public string pacUrl;
        public bool useOnlinePac;

        public const string CONFIG_FILE = "gui-config.json";

        public Server GetCurrentServer()
        {
            if (index >= 0 && index < configs.Count)
                return configs[index];
            else
                return GetDefaultServer();
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckServer(server.server);
        }

        public static OfficialConfig Load()
        {
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                OfficialConfig config = Utils.DeSerializeJsonObject<OfficialConfig>(configContent);
                config.isDefault = false;
                if (config.localPort == 0)
                    config.localPort = 1080;
                if (config.index == -1)
                    config.index = 0;
                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Logging.LogUsefulException(e);
                return new OfficialConfig
                {
                    index = 0,
                    isDefault = true,
                    localPort = 1080,
                    configs = new List<Server>()
                    {
                        GetDefaultServer()
                    }
                };
            }
        }

        public static void Save(OfficialConfig config)
        {
            if (config.index >= config.configs.Count)
                config.index = config.configs.Count - 1;
            if (config.index < -1)
                config.index = -1;
            if (config.index == -1)
                config.index = 0;
            config.isDefault = false;
            try
            {
                File.WriteAllText(CONFIG_FILE,Utils.SerializeToJsonString(config));
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception("assertion failure");
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port out of range");
        }

        public static void CheckLocalPort(int port)
        {
            CheckPort(port);
            if (port == 8123)
                throw new ArgumentException("Port can't be 8123");
        }

        private static void CheckPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password can not be blank");
        }

        private static void CheckServer(string server)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentException("Server IP can not be blank");
        }
    }
}
