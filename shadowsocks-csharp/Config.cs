using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using SimpleJson;

namespace shadowsocks_csharp
{
    [Serializable]
    public class Config
    {
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
                    local_port = 1081,
                    password = "barfoo!",
                    method = "table",
                    isDefault = true
                };
            }
        }

        public static void Save(Config config)
        {
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
                        method = config.method
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
