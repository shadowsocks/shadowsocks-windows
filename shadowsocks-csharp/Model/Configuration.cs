using Shadowsocks.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        public List<Server> configs;

        // when strategy is set, index is ignored
        public string strategy;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public int localPort;
        public string pacUrl;
        public bool useOnlinePac;

        private static string CONFIG_FILE = "gui-config.json";

        public Server GetCurrentServer()
        {
            if (index >= 0 && index < configs.Count)
            {
                return configs[index];
            }
            else
            {
                return GetDefaultServer();
            }
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckServer(server.server);
        }

        public static Configuration Load()
        {
            try
            {
                string configContent = File.ReadAllText(CONFIG_FILE);
                Configuration config = SimpleJson.SimpleJson.DeserializeObject<Configuration>(configContent, new JsonSerializerStrategy());
                config.isDefault = false;
                if (config.localPort == 0)
                {
                    config.localPort = 1080;
                }
                if (config.index == -1)
                {
                    if (config.strategy == null)
                    {
                        config.index = 0;
                    }
                }
                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                {
                    Console.WriteLine(e);
                }
                return new Configuration
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

        public static void Save(Configuration config)
        {
            if (config.index >= config.configs.Count)
            {
                config.index = config.configs.Count - 1;
            }
            if (config.index < -1)
            {
                config.index = -1;
            }
            if (config.index == -1)
            {
                if (config.strategy == null)
                {
                    config.index = 0;
                }
            }
            config.isDefault = false;
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(CONFIG_FILE, FileMode.Create)))
                {
                    string jsonString = SimpleJson.SimpleJson.SerializeObject(config);
                    sw.Write(jsonString);
                    sw.Flush();
                }
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
            {
                throw new Exception(I18N.GetString("assertion failure"));
            }
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
            {
                throw new ArgumentException(I18N.GetString("Port out of range"));
            }
        }

        public static void CheckLocalPort(int port)
        {
            CheckPort(port);
            if (port == 8123)
            {
                throw new ArgumentException(I18N.GetString("Port can't be 8123"));
            }
        }

        private static void CheckPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException(I18N.GetString("Password can not be blank"));
            }
        }

        private static void CheckServer(string server)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new ArgumentException(I18N.GetString("Server IP can not be blank"));
            }
        }

        private class JsonSerializerStrategy : SimpleJson.PocoJsonSerializerStrategy
        {
            // convert string to int
            public override object DeserializeObject(object value, Type type)
            {
                if (type == typeof(Int32) && value.GetType() == typeof(string))
                {
                    return Int32.Parse(value.ToString());
                }
                return base.DeserializeObject(value, type);
            }
        }
    }
}
