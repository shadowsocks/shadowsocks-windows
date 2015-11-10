using Shadowsocks.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Shadowsocks.Model
{
    public class ServerSelectStrategy
    {
        private Random randomGennarator;
        private int lastSelectIndex;
        private DateTime lastSelectTime;
        private int lastUserSelectIndex;

        private struct ServerIndex
        {
            public int index;
            public Server server;
            public ServerIndex(int i, Server s)
            {
                index = i;
                this.server = s;
            }
        };
        private int lowerBound(List<double> data, double target)
        {
            int left = 0;
            int right = data.Count - 1;
            while (left < right)
            {
                int mid = (left + right) / 2;
                if (data[mid] >= target)
                    right = mid;
                else if (data[mid] < target)
                    left = mid + 1;
            }
            return left;
        }

        private double Algorithm2(ServerSpeedLog serverSpeedLog)
        {
            if (serverSpeedLog.ErrorContinurousTimes > 30)
                return 1;
            else if (serverSpeedLog.TotalConnectTimes < 3)
                return 500;
            else if (serverSpeedLog.AvgConnectTime < 0)
                return 1;
            else if (serverSpeedLog.AvgConnectTime <= 20)
                return 500;
            else
            {
                long connections = serverSpeedLog.TotalConnectTimes - serverSpeedLog.TotalDisconnectTimes;
                double chance = 10000.0 / serverSpeedLog.AvgConnectTime - connections * 5;
                if (chance > 500) chance = 500;
                chance -= serverSpeedLog.ErrorContinurousTimes * 10;
                if (chance < 1) chance = 1;
                return chance;
            }
        }

        private double Algorithm3(ServerSpeedLog serverSpeedLog)
        {
            if (serverSpeedLog.ErrorContinurousTimes > 30)
                return 1;
            else if (serverSpeedLog.TotalConnectTimes < 3)
                return 500;
            else if (serverSpeedLog.AvgConnectTime < 0)
                return 1;
            else
            {
                long connections = serverSpeedLog.TotalConnectTimes - serverSpeedLog.TotalDisconnectTimes;
                double chance = 20.0 / (serverSpeedLog.AvgConnectTime / 500 + 1) - connections;
                if (chance > 500) chance = 500;
                chance -= serverSpeedLog.ErrorContinurousTimes * 2;
                if (chance < 1) chance = 1;
                return chance;
            }
        }

        public int Select(List<Server> configs, int curIndex, int algorithm, bool forceChange = false)
        {
            if (randomGennarator == null)
            {
                randomGennarator = new Random();
                lastSelectIndex = -1;
            }
            if (configs.Count <= lastSelectIndex || lastSelectIndex < 0 || !configs[lastSelectIndex].isEnable())
            {
                lastSelectIndex = -1;
                lastSelectTime = DateTime.Now;
                lastUserSelectIndex = -1;
            }
            if (lastUserSelectIndex != curIndex)
            {
                if (configs.Count > curIndex && curIndex >= 0 && configs[curIndex].isEnable())
                {
                    lastSelectIndex = curIndex;
                }
                lastUserSelectIndex = curIndex;
            }
            if (configs.Count > 0)
            {
                List<ServerIndex> serverList = new List<ServerIndex>();
                for (int i = 0; i < configs.Count; ++i)
                {
                    if (forceChange && lastSelectIndex == i)
                        continue;
                    if (configs[i].isEnable())
                        serverList.Add(new ServerIndex(i, configs[i]));
                }
                if (serverList.Count == 0)
                {
                    int i = lastSelectIndex;
                    if (configs[i].isEnable())
                        serverList.Add(new ServerIndex(i, configs[i]));
                }
                int serverListIndex = -1;
                if (serverList.Count > 0)
                {
                    if (algorithm == 0)
                    {
                        lastSelectIndex = (lastSelectIndex + 1) % configs.Count;
                        for (int i = 0; i < configs.Count; ++i)
                        {
                            if (configs[lastSelectIndex].isEnable())
                            {
                                serverListIndex = lastSelectIndex;
                                break;
                            }
                            else
                            {
                                lastSelectIndex = (lastSelectIndex + 1) % configs.Count;
                            }
                        }
                    }
                    else if (algorithm == 1)
                    {
                        serverListIndex = randomGennarator.Next(serverList.Count);
                        serverListIndex = serverList[serverListIndex].index;
                    }
                    else if (algorithm == 3 || algorithm == 5)
                    {
                        if (algorithm == 5)
                        {
                            if ((DateTime.Now - lastSelectTime).TotalSeconds > 60 * 10)
                            {
                                lastSelectTime = DateTime.Now;
                            }
                            else
                            {
                                if (configs.Count > lastSelectIndex && lastSelectIndex >= 0 && configs[lastSelectIndex].isEnable() && !forceChange)
                                {
                                    return lastSelectIndex;
                                }
                            }
                        }
                        List<double> chances = new List<double>();
                        double lastBeginVal = 0;
                        foreach (ServerIndex s in serverList)
                        {
                            double chance = Algorithm3(s.server.ServerSpeedLog());
                            chances.Add(lastBeginVal + chance);
                            lastBeginVal += chance;
                        }
                        {
                            double target = randomGennarator.NextDouble() * lastBeginVal;
                            serverListIndex = lowerBound(chances, target);
                            serverListIndex = serverList[serverListIndex].index;
                            lastSelectIndex = serverListIndex;
                            return serverListIndex;
                        }
                    }
                    else //if (algorithm == 2 || algorithm == 4)
                    {
                        List<double> chances = new List<double>();
                        double lastBeginVal = 0;
                        foreach (ServerIndex s in serverList)
                        {
                            double chance = Algorithm2(s.server.ServerSpeedLog());
                            chances.Add(lastBeginVal + chance);
                            lastBeginVal += chance;
                        }
                        if (algorithm == 4 && randomGennarator.Next(3) == 0)
                        {
                            lastSelectIndex = curIndex;
                            return curIndex;
                        }
                        {
                            double target = randomGennarator.NextDouble() * lastBeginVal;
                            serverListIndex = lowerBound(chances, target);
                            serverListIndex = serverList[serverListIndex].index;
                            lastSelectIndex = serverListIndex;
                            return serverListIndex;
                        }
                    }
                }
                lastSelectIndex = serverListIndex;
                return serverListIndex;
            }
            else
            {
                return -1;
            }
        }
    }

    [Serializable]
    public class Configuration
    {
        public List<Server> configs;
        public int index;
        public bool random;
        public bool global;
        public bool enabled;
        public bool shareOverLan;
        public bool isDefault;
        public int localPort;
        public string pacUrl;
        public bool useOnlinePac;
        public int reconnectTimes;
        public int randomAlgorithm;
        public int TTL;
        public bool socks5enable;
        public string socks5Host;
        public int socks5Port;
        public string socks5User;
        public string socks5Pass;
        public string authUser;
        public string authPass;
        public bool autoban;
        public bool buildinHttpProxy;
        private ServerSelectStrategy serverStrategy = new ServerSelectStrategy();

        private static string CONFIG_FILE = "gui-config.json";

        public Server GetCurrentServer(bool usingRandom = false, bool forceRandom = false)
        {
            lock (serverStrategy)
            {
                if (forceRandom)
                {
                    int index = serverStrategy.Select(configs, this.index, randomAlgorithm, true);
                    if (index == -1) return GetDefaultServer();
                    return configs[index];
                }
                else if (usingRandom && random)
                {
                    int index = serverStrategy.Select(configs, this.index, randomAlgorithm);
                    if (index == -1) return GetDefaultServer();
                    return configs[index];
                }
                else
                {
                    if (index >= 0 && index < configs.Count)
                    {
                        int selIndex = index;
                        if (usingRandom)
                        {
                            for (int i = 0; i < configs.Count; ++i)
                            {
                                if (configs[selIndex].isEnable())
                                {
                                    break;
                                }
                                else
                                {
                                    selIndex = (selIndex + 1) % configs.Count;
                                }
                            }
                        }

                        return configs[selIndex];
                    }
                    else
                    {
                        return GetDefaultServer();
                    }
                }
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
                // revert base64 encode for version 3.5.4
                {
                    int base64_encode = 0;
                    foreach (var server in config.configs)
                    {
                        string remarks = server.remarks;
                        if (remarks.Length == 0)
                            continue;
                        if (server.remarks[remarks.Length - 1] == '=')
                        {
                            server.remarks_base64 = remarks;
                            if (server.remarks_base64 == server.remarks)
                            {
                                server.remarks = remarks;
                                base64_encode = 0;
                                break;
                            }
                            else
                            {
                                base64_encode++;
                            }
                            server.remarks = remarks;
                        }
                    }
                    if (base64_encode > 0)
                    {
                        foreach (var server in config.configs)
                        {
                            string remarks = server.remarks;
                            if (remarks.Length == 0)
                                continue;
                            server.remarks_base64 = remarks;
                        }
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
                    reconnectTimes = 3,
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
