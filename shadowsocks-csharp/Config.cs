using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;

namespace shadowsocks_csharp
{
    [Serializable]
    public class Config
    {
        public string server;
        public int server_port;
        public int local_port;
        public string password;

        public static Config Load()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Config));
            try
            {
                using (FileStream fs = File.OpenRead(@"config.json"))
                {
                    Config config = ser.ReadObject(fs) as Config;
                    return config;
                }
            }
            catch (IOException e)
            {
                return new Config
                {
                    server = "127.0.0.1",
                    server_port = 8388,
                    local_port = 1080,
                    password = "foobar!"
                };
            }
        }

        public static void Save(Config config)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Config));
            try
            {
                using (FileStream fs = File.Open(@"config.json",FileMode.Create))
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
