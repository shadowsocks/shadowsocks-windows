using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using SimpleJson;

namespace shadowsocks_csharp.Model
{
    [Serializable]
    public class Config
    {
        public bool enabled;
        public string server;
        public int server_port;
        public int local_port;
        public string password;
        public string method;

        public bool isDefault;

        private static void assert(bool condition)
        {
            if(!condition) 
            {
                throw new Exception("assertion failure");
            }
        }

        public static Config Load()
        {
            try
            {
                using (StreamReader sr = new StreamReader(File.OpenRead(@"config.json")))
                {
                    Config config = SimpleJson.SimpleJson.DeserializeObject<Config>(sr.ReadToEnd());
                    assert(!string.IsNullOrEmpty(config.server));
                    assert(!string.IsNullOrEmpty(config.password));
                    assert(config.local_port > 0);
                    assert(config.server_port > 0);
                    config.isDefault = false;
                    return config;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new Config
                {
                    server = "127.0.0.1",
                    server_port = 8388,
                    local_port = 1080,
                    password = "barfoo!",
                    method = "table",
                    enabled = true,
                    isDefault = true
                };
            }
        }

        private static void checkPort(int port)
        {
            if (port <= 0 || port > 65535)
            {
                throw new ArgumentException("port out of range");
            }
        }

        private static void checkPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("password can not be blank");
            }
        }

        private static void checkServer(string server)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new ArgumentException("server can not be blank");
            }
        }

        public static void Save(Config config)
        {
            checkPort(config.local_port);
            checkPort(config.server_port);
            checkPassword(config.password);
            checkServer(config.server);
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(@"config.json", FileMode.Create)))
                {
                    string jsonString = SimpleJson.SimpleJson.SerializeObject(new
                    {
                        server = config.server,
                        server_port = config.server_port,
                        local_port = config.local_port,
                        password = config.password,
                        method = config.method,
                        enabled = config.enabled
                    });
                    sw.Write(jsonString);
                    sw.Flush();
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
