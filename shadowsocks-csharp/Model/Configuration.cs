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

        enum SelectAlgorithm
        {
            OneByOne,
            Random,
            LowLatency,
            LowException,
            SelectedFirst,
            Timer,
            LowExceptionInGroup,
        }

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
                    {
                        if (algorithm == (int)SelectAlgorithm.LowExceptionInGroup
                            && configs.Count > lastSelectIndex && lastSelectIndex >= 0
                            && configs[lastSelectIndex].group != configs[i].group)
                        {
                            continue;
                        }
                        serverList.Add(new ServerIndex(i, configs[i]));
                    }
                }
                if (serverList.Count == 0)
                {
                    int i = lastSelectIndex;
                    if (i >= 0 && i < configs.Count && configs[i].isEnable())
                        serverList.Add(new ServerIndex(i, configs[i]));
                }
                int serverListIndex = -1;
                if (serverList.Count > 0)
                {
                    if (algorithm == (int)SelectAlgorithm.OneByOne)
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
                    else if (algorithm == (int)SelectAlgorithm.Random)
                    {
                        serverListIndex = randomGennarator.Next(serverList.Count);
                        serverListIndex = serverList[serverListIndex].index;
                    }
                    else if (algorithm == (int)SelectAlgorithm.LowException
                        || algorithm == (int)SelectAlgorithm.Timer
                        || algorithm == (int)SelectAlgorithm.LowExceptionInGroup)
                    {
                        if (algorithm == (int)SelectAlgorithm.Timer)
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
                    else //if (algorithm == (int)SelectAlgorithm.LowLatency || algorithm == (int)SelectAlgorithm.SelectedFirst)
                    {
                        List<double> chances = new List<double>();
                        double lastBeginVal = 0;
                        foreach (ServerIndex s in serverList)
                        {
                            double chance = Algorithm2(s.server.ServerSpeedLog());
                            chances.Add(lastBeginVal + chance);
                            lastBeginVal += chance;
                        }
                        if (algorithm == (int)SelectAlgorithm.SelectedFirst && randomGennarator.Next(3) == 0 && configs[curIndex].isEnable())
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

    public class UriVisitTime : IComparable
    {
        public DateTime visitTime;
        public string uri;
        public int index;

        public int CompareTo(object other)
        {
            if (!(other is UriVisitTime))
                throw new InvalidOperationException("CompareTo: Not a UriVisitTime");
            if (Equals(other))
                return 0;
            return visitTime.CompareTo(((UriVisitTime)other).visitTime);
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
        public bool bypassWhiteList;
        public int localPort;
        public int reconnectTimes;
        public int randomAlgorithm;
        public int TTL;
        public bool proxyEnable;
        public bool pacDirectGoProxy;
        public int proxyType;
        public string proxyHost;
        public int proxyPort;
        public string proxyAuthUser;
        public string proxyAuthPass;
        public string authUser;
        public string authPass;
        public bool autoBan;
        public bool sameHostForSameTarget;

        //public bool buildinHttpProxy;
        private ServerSelectStrategy serverStrategy = new ServerSelectStrategy();
        private Dictionary<string, UriVisitTime> uri2time = new Dictionary<string, UriVisitTime>();
        private SortedDictionary<UriVisitTime, string> time2uri = new SortedDictionary<UriVisitTime, string>();

        private static string CONFIG_FILE = "gui-config.json";

        public Server GetCurrentServer(string targetAddr = null, bool usingRandom = false, bool forceRandom = false)
        {
            lock (serverStrategy)
            {
                foreach (KeyValuePair<UriVisitTime, string> p in time2uri)
                {
                    if ((DateTime.Now - p.Key.visitTime).TotalSeconds < 60)
                        break;

                    uri2time.Remove(p.Value);
                    time2uri.Remove(p.Key);
                    break;
                }
                if (sameHostForSameTarget && !forceRandom && targetAddr != null && uri2time.ContainsKey(targetAddr))
                {
                    UriVisitTime visit = uri2time[targetAddr];
                    if (visit.index < configs.Count && configs[visit.index].enable)
                    {
                        //uri2time.Remove(targetURI);
                        time2uri.Remove(visit);
                        visit.visitTime = DateTime.Now;
                        uri2time[targetAddr] = visit;
                        time2uri[visit] = targetAddr;
                        return configs[visit.index];
                    }
                }
                if (forceRandom)
                {
                    int index = serverStrategy.Select(configs, this.index, randomAlgorithm, true);
                    if (index == -1) return GetErrorServer();
                    if (targetAddr != null)
                    {
                        UriVisitTime visit = new UriVisitTime();
                        visit.uri = targetAddr;
                        visit.index = index;
                        visit.visitTime = DateTime.Now;
                        if (uri2time.ContainsKey(targetAddr))
                        {
                            time2uri.Remove(uri2time[targetAddr]);
                        }
                        uri2time[targetAddr] = visit;
                        time2uri[visit] = targetAddr;
                    }
                    return configs[index];
                }
                else if (usingRandom && random)
                {
                    int index = serverStrategy.Select(configs, this.index, randomAlgorithm);
                    if (index == -1) return GetErrorServer();
                    if (targetAddr != null)
                    {
                        UriVisitTime visit = new UriVisitTime();
                        visit.uri = targetAddr;
                        visit.index = index;
                        visit.visitTime = DateTime.Now;
                        if (uri2time.ContainsKey(targetAddr))
                        {
                            time2uri.Remove(uri2time[targetAddr]);
                        }
                        uri2time[targetAddr] = visit;
                        time2uri[visit] = targetAddr;
                    }
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

                        if (targetAddr != null)
                        {
                            UriVisitTime visit = new UriVisitTime();
                            visit.uri = targetAddr;
                            visit.index = selIndex;
                            visit.visitTime = DateTime.Now;
                            if (uri2time.ContainsKey(targetAddr))
                            {
                                time2uri.Remove(uri2time[targetAddr]);
                            }
                            uri2time[targetAddr] = visit;
                            time2uri[visit] = targetAddr;
                        }
                        return configs[selIndex];
                    }
                    else
                    {
                        return GetErrorServer();
                    }
                }
            }
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.server_port);
            if (server.server_udp_port != 0)
                CheckPort(server.server_udp_port);
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

        public static Server GetErrorServer()
        {
            Server server = new Server();
            server.server = "invalid";
            return server;
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

    [Serializable]
    public class ServerTrans
    {
        public long totalUploadBytes;
        public long totalDownloadBytes;

        void AddUpload(long bytes)
        {
            //lock (this)
            {
                totalUploadBytes += bytes;
            }
        }
        void AddDownload(long bytes)
        {
            //lock (this)
            {
                totalDownloadBytes += bytes;
            }
        }
    }

    [Serializable]
    public class ServerTransferTotal
    {
        private static string LOG_FILE = "transfer_log.json";

        public Dictionary<String, ServerTrans> servers = new Dictionary<String, ServerTrans>();
        private int saveCounter;
        private DateTime saveTime;

        public static ServerTransferTotal Load()
        {
            try
            {
                string configContent = File.ReadAllText(LOG_FILE);
                ServerTransferTotal config = SimpleJson.SimpleJson.DeserializeObject<ServerTransferTotal>(configContent, new JsonSerializerStrategy());
                config.Init();
                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                {
                    Console.WriteLine(e);
                }
                return new ServerTransferTotal();
            }
        }

        public void Init()
        {
            saveCounter = 256;
            saveTime = DateTime.Now;
            if (servers == null)
                servers = new Dictionary<String, ServerTrans>();
        }

        public static void Save(ServerTransferTotal config)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(LOG_FILE, FileMode.Create)))
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

        public void AddUpload(string server, Int64 size)
        {
            lock (servers)
            {
                if (!servers.ContainsKey(server))
                    servers.Add(server, new ServerTrans());
                servers[server].totalUploadBytes += size;
            }
            if (--saveCounter <= 0)
            {
                saveCounter = 256;
                if ((DateTime.Now - saveTime).TotalMinutes > 10 )
                {
                    lock (servers)
                    {
                        Save(this);
                        saveTime = DateTime.Now;
                    }
                }
            }
        }
        public void AddDownload(string server, Int64 size)
        {
            lock (servers)
            {
                if (!servers.ContainsKey(server))
                    servers.Add(server, new ServerTrans());
                servers[server].totalDownloadBytes += size;
            }
            if (--saveCounter <= 0)
            {
                saveCounter = 256;
                if ((DateTime.Now - saveTime).TotalMinutes > 10 )
                {
                    lock (servers)
                    {
                        Save(this);
                        saveTime = DateTime.Now;
                    }
                }
            }
        }

        private class JsonSerializerStrategy : SimpleJson.PocoJsonSerializerStrategy
        {
            public override object DeserializeObject(object value, Type type)
            {
                if (type == typeof(Int64) && value.GetType() == typeof(string))
                {
                    return Int64.Parse(value.ToString());
                }
                else if (type == typeof(ServerTransferTotal))
                {
                    ServerTransferTotal transfer = new ServerTransferTotal();
                    foreach (KeyValuePair<string, object> kv in ((IDictionary<string, object>)value))
                    {
                        foreach (IDictionary<string, object> kv_sub in (SimpleJson.JsonArray)(kv.Value))
                        {
                            transfer.servers.Add((string)kv_sub["Key"], (ServerTrans)base.DeserializeObject(kv_sub["Value"], typeof(ServerTrans)));
                        }
                        break;
                    }
                    return transfer;
                }
                return base.DeserializeObject(value, type);
            }
        }

    }
}
