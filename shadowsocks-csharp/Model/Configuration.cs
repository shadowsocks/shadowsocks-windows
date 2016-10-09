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
        private const int MAX_CHANCE = 10000;
        private const int ERROR_PENALTY = MAX_CHANCE / 20;
        private const int CONNECTION_PENALTY = MAX_CHANCE / 100;
        private const int MIN_CHANCE = 10;

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

        private double Algorithm2(ServerSpeedLog serverSpeedLog) // perfer less delay
        {
            if (serverSpeedLog.ErrorContinurousTimes >= 20)
                return 1;
            else if (serverSpeedLog.ErrorContinurousTimes >= 10)
                return MIN_CHANCE;
            else if (serverSpeedLog.AvgConnectTime < 0 && serverSpeedLog.TotalConnectTimes >= 3)
                return MIN_CHANCE;
            else if (serverSpeedLog.TotalConnectTimes < 1)
                return MAX_CHANCE;
            else
            {
                long avgConnectTime = serverSpeedLog.AvgConnectTime <= 0 ? 1 : serverSpeedLog.AvgConnectTime;
                if (serverSpeedLog.TotalConnectTimes >= 1 && serverSpeedLog.AvgConnectTime < 0)
                    avgConnectTime = 5000;
                long connections = serverSpeedLog.TotalConnectTimes - serverSpeedLog.TotalDisconnectTimes;
                double chance = MAX_CHANCE * 10.0 / avgConnectTime - connections * CONNECTION_PENALTY;
                if (chance > MAX_CHANCE) chance = MAX_CHANCE;
                chance -= serverSpeedLog.ErrorContinurousTimes * ERROR_PENALTY;
                if (chance < MIN_CHANCE) chance = MIN_CHANCE;
                return chance;
            }
        }

        private double Algorithm3(ServerSpeedLog serverSpeedLog) // perfer less error
        {
            if (serverSpeedLog.ErrorContinurousTimes >= 20)
                return 1;
            else if (serverSpeedLog.ErrorContinurousTimes >= 10)
                return MIN_CHANCE;
            else if (serverSpeedLog.AvgConnectTime < 0 && serverSpeedLog.TotalConnectTimes >= 3)
                return MIN_CHANCE;
            else if (serverSpeedLog.TotalConnectTimes < 1)
                return MAX_CHANCE;
            else
            {
                long avgConnectTime = serverSpeedLog.AvgConnectTime <= 0 ? 1 : serverSpeedLog.AvgConnectTime;
                if (serverSpeedLog.TotalConnectTimes >= 1 && serverSpeedLog.AvgConnectTime < 0)
                    avgConnectTime = 5000;
                long connections = serverSpeedLog.TotalConnectTimes - serverSpeedLog.TotalDisconnectTimes;
                double chance = MAX_CHANCE * 1.0 / (avgConnectTime / 500 + 1) - connections * CONNECTION_PENALTY;
                if (chance > MAX_CHANCE) chance = MAX_CHANCE;
                chance -= serverSpeedLog.ErrorContinurousTimes * ERROR_PENALTY;
                if (chance < MIN_CHANCE) chance = MIN_CHANCE;
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
                            if (chance > 0)
                            {
                                chances.Add(lastBeginVal + chance);
                                lastBeginVal += chance;
                            }
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
                            if (chance > 0)
                            {
                                chances.Add(lastBeginVal + chance);
                                lastBeginVal += chance;
                            }
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
    public class PortMapConfig
    {
        public static int MemberCount = 4;

        public bool enable;
        public string id;
        public string server_addr;
        public int server_port;
    }

    public class PortMapConfigCache
    {
        public string id;
        public Server server;
        public string server_addr;
        public int server_port;
    }

    [Serializable]
    public class Configuration
    {
        public List<Server> configs;
        public int index;
        public bool random;
        public int sysProxyMode;
        public bool shareOverLan;
        public bool bypassWhiteList;
        public int localPort;
        public int reconnectTimes;
        public int randomAlgorithm;
        public int TTL;
        public int connect_timeout;
        public bool proxyEnable;
        public bool pacDirectGoProxy;
        public int proxyType;
        public string proxyHost;
        public int proxyPort;
        public string proxyAuthUser;
        public string proxyAuthPass;
        public string proxyUserAgent;
        public string authUser;
        public string authPass;
        public bool autoBan;
        public bool sameHostForSameTarget;
        public int keepVisitTime;
        public bool isHideTips;
        public string dns_server;
        public int proxyRuleMode;
        public Dictionary<string, string> token = new Dictionary<string, string>();
        public Dictionary<string, object> portMap = new Dictionary<string, object>();

        private ServerSelectStrategy serverStrategy = new ServerSelectStrategy();
        private Dictionary<string, UriVisitTime> uri2time = new Dictionary<string, UriVisitTime>();
        private SortedDictionary<UriVisitTime, string> time2uri = new SortedDictionary<UriVisitTime, string>();
        private Dictionary<int, PortMapConfigCache> portMapCache = new Dictionary<int, PortMapConfigCache>();

        private static string CONFIG_FILE = "gui-config.json";

        public bool KeepCurrentServer(string targetAddr, string id)
        {
            if (sameHostForSameTarget && targetAddr != null)
            {
                lock (serverStrategy)
                {
                    if (uri2time.ContainsKey(targetAddr))
                    {
                        UriVisitTime visit = uri2time[targetAddr];
                        int index = -1;
                        for (int i = 0; i < configs.Count; ++i)
                        {
                            if (configs[i].id == id)
                            {
                                index = i;
                                break;
                            }
                        }
                        if (index >= 0 && configs[index].enable)
                        {
                            time2uri.Remove(visit);
                            visit.index = index;
                            visit.visitTime = DateTime.Now;
                            uri2time[targetAddr] = visit;
                            time2uri[visit] = targetAddr;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public Server GetCurrentServer(string targetAddr = null, bool usingRandom = false, bool forceRandom = false)
        {
            lock (serverStrategy)
            {
                foreach (KeyValuePair<UriVisitTime, string> p in time2uri)
                {
                    if ((DateTime.Now - p.Key.visitTime).TotalSeconds < keepVisitTime)
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
                    //if (targetAddr != null)
                    //{
                    //    UriVisitTime visit = new UriVisitTime();
                    //    visit.uri = targetAddr;
                    //    visit.index = index;
                    //    visit.visitTime = DateTime.Now;
                    //    if (uri2time.ContainsKey(targetAddr))
                    //    {
                    //        time2uri.Remove(uri2time[targetAddr]);
                    //    }
                    //    uri2time[targetAddr] = visit;
                    //    time2uri[visit] = targetAddr;
                    //}
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

        public void FlushPortMapCache()
        {
            portMapCache = new Dictionary<int, PortMapConfigCache>();
            Dictionary<string, Server> id2server = new Dictionary<string, Server>();
            foreach (Server s in configs)
            {
                id2server[s.id] = s;
            }
            foreach (KeyValuePair<string, object> pair in portMap)
            {
                int key = 0;
                PortMapConfig pm = (PortMapConfig)pair.Value;
                if (!pm.enable)
                    continue;
                if (!id2server.ContainsKey(pm.id))
                    continue;
                try
                {
                    key = int.Parse(pair.Key);
                }
                catch (FormatException)
                {
                    continue;
                }
                portMapCache[key] = new PortMapConfigCache();
                portMapCache[key].id = pm.id;
                portMapCache[key].server = id2server[pm.id];
                portMapCache[key].server_addr = pm.server_addr;
                portMapCache[key].server_port = pm.server_port;
            }
        }

        public Dictionary<int, PortMapConfigCache> GetPortMapCache()
        {
            return portMapCache;
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.server_port);
            if (server.server_udp_port != 0)
                CheckPort(server.server_udp_port);
            CheckPassword(server.password);
            CheckServer(server.server);
        }

        public Configuration()
        {
            index = 0;
            localPort = 1080;
            reconnectTimes = 2;
            keepVisitTime = 180;
            connect_timeout = 5;
            configs = new List<Server>()
            {
                GetDefaultServer()
            };
            portMap = new Dictionary<string, object>();
        }

        public static Configuration LoadFile(string filename)
        {
            try
            {
                string configContent = File.ReadAllText(filename);
                Configuration config = SimpleJson.SimpleJson.DeserializeObject<Configuration>(configContent, new JsonSerializerStrategy());
                if (config.localPort == 0)
                {
                    config.localPort = 1080;
                }
                if (config.keepVisitTime == 0)
                {
                    config.keepVisitTime = 180;
                }
                if (config.portMap == null)
                {
                    config.portMap = new Dictionary<string, object>();
                }
                if (config.token == null)
                {
                    config.token = new Dictionary<string, string>();
                }
                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                {
                    Console.WriteLine(e);
                }
                return new Configuration();
            }
        }

        public static Configuration Load()
        {
            return LoadFile(CONFIG_FILE);
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

        public Configuration Load(string config_str)
        {
            try
            {
                Configuration config = SimpleJson.SimpleJson.DeserializeObject<Configuration>(config_str, new JsonSerializerStrategy());
                return config;
            }
            catch
            {
            }
            return null;
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        public static Server CopyServer(Server server)
        {
            Server s = new Server();
            s.server = server.server;
            s.server_port = server.server_port;
            s.method = server.method;
            s.protocol = server.protocol;
            s.obfs = server.obfs;
            s.obfsparam = server.obfsparam??"";
            s.password = server.password;
            s.remarks = server.remarks;
            s.group = server.group;
            s.udp_over_tcp = server.udp_over_tcp;
            s.server_udp_port = server.server_udp_port;
            return s;
        }

        public static Server GetErrorServer()
        {
            Server server = new Server();
            server.server = "invalid";
            return server;
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
                else if (type == typeof(object) && value.GetType() == typeof(SimpleJson.JsonObject) && ((SimpleJson.JsonObject)value).Count == PortMapConfig.MemberCount)
                {
                    return base.DeserializeObject(value, typeof(PortMapConfig));
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

        public Dictionary<string, object> servers = new Dictionary<string, object>();
        private int saveCounter;
        private DateTime saveTime;

        public static ServerTransferTotal Load()
        {
            try
            {
                string configContent = File.ReadAllText(LOG_FILE);
                ServerTransferTotal config = new ServerTransferTotal();
                config.servers = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, object>>(configContent, new JsonSerializerStrategy());
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
                servers = new Dictionary<string, object>();
        }

        public static void Save(ServerTransferTotal config)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(LOG_FILE, FileMode.Create)))
                {
                    string jsonString = SimpleJson.SimpleJson.SerializeObject(config.servers);
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
                ((ServerTrans)servers[server]).totalUploadBytes += size;
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
                ((ServerTrans)servers[server]).totalDownloadBytes += size;
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
                else if (type == typeof(object))
                {
                    return base.DeserializeObject(value, typeof(ServerTrans));
                }
                return base.DeserializeObject(value, type);
            }
        }

    }
}
