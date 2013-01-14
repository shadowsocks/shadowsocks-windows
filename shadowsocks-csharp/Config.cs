using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace shadowsocks_csharp
{
    [Serializable]
    public class Config
    {
        public string server;
        public int server_port;
        public int local_port;
        public string password;

        [NonSerialized]
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
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Config));
            try
            {
                using (FileStream fs = File.OpenRead(@"config.json"))
                {
                    Config config = ser.ReadObject(fs) as Config;
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
                    isDefault = true
                };
            }
        }

        public static void Save(Config config)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Config));
            try
            {
                using (FileStream fs = File.Open(@"config.json", FileMode.Create))
                {
                    ser.WriteObject(fs, config);
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
