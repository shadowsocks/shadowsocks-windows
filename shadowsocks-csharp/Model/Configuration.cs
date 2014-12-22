using Shadowsocks.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        public List<Server> configs;
        public int index;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;

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
            CheckPort(server.local_port);
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
            if (config.index < 0)
            {
                config.index = 0;
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
            return new Server()
            {
                server = "",
                server_port = 8388,
                local_port = 1080,
                method = "aes-256-cfb",
                password = "",
                remarks = ""
            };
        }

        private static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new Exception(I18N.GetString("assertion failure"));
            }
        }

        private static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
            {
                throw new ArgumentException(I18N.GetString("Port out of range"));
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
        private static Server parse_ss_uri(string uri)
        {
            string[] sArray = uri.Split(new char[2] { ':', '@' });

            Server server = Configuration.GetDefaultServer();
            server.method = sArray[0].ToString();
            server.password = sArray[1].ToString();
            server.server = sArray[2].ToString();
            server.server_port = int.Parse(sArray[3].ToString());
            return server;
        }
        public static bool parse_uri(string uri, ref Server server)
        {
            Regex regex_ss_head = new Regex("^(?i:(ss://))");
            Regex regex_ss = new Regex("^(?i:(aes-256-cfb|aes-128-cfb|aes-192-cfb|aes-256-ofb|aes-128-ofb|aes-192-ofb|aes-128-ctr|aes-192-ctr|aes-256-ctr|aes-128-cfb8|aes-192-cfb8|aes-256-cfb8|aes-128-cfb1|aes-192-cfb1|aes-256-cfb1|bf-cfb|camellia-128-cfb|camellia-192-cfb|camellia-256-cfb|cast5-cfb|des-cfb|idea-cfb|rc2-cfb|rc4-md5|seed-cfb|salsa20-ctr|rc4|table)):[a-zA-Z0-9\\.\\-]+@((25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])|([a-zA-Z0-9\\-]+\\.)*[a-zA-Z0-9\\-]+\\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{2}))\\:[0-9]+");
            Regex regexbase64 = new Regex("^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?");

            string uri_core = "";

            try
            {

                if (regex_ss_head.IsMatch(uri))
                {
                    string[] sArray = Regex.Split(uri, "://", RegexOptions.IgnoreCase);
                    uri_core = sArray[1].ToString();
                }
                else
                {
                    uri_core = uri;
                }

                if (regex_ss.IsMatch(uri_core))
                {
                    server = parse_ss_uri(uri_core);
                    return true;
                }
                else
                {
                    if (regexbase64.IsMatch(uri_core))
                    {
                        byte[] arr = System.Convert.FromBase64String(uri_core);
                        string uri_core2 = System.Text.Encoding.UTF8.GetString(arr);
                        if (regex_ss.IsMatch(uri_core2))
                        {
                            server = parse_ss_uri(uri_core2);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
            return false;

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
