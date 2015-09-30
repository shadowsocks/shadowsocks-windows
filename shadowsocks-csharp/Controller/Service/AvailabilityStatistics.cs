using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    using DataUnit = KeyValuePair<string, string>;
    using DataList = List<KeyValuePair<string, string>>;

    using Statistics = Dictionary<string, List<AvailabilityStatistics.RawStatisticsData>>;

    public class AvailabilityStatistics
    {
        public static readonly string DateTimePattern = "yyyy-MM-dd HH:mm:ss";
        private const string StatisticsFilesName = "shadowsocks.availability.csv";
        private const string Delimiter = ",";
        private const int Timeout = 500;
        private const int DelayBeforeStart = 1000;
        public Statistics RawStatistics { get; private set; }
        public Statistics FilteredStatistics { get; private set; }
        public static readonly DateTime UnknownDateTime = new DateTime(1970, 1, 1);
        private int Repeat => _config.RepeatTimesNum;
        private const int RetryInterval = 2*60*1000; //retry 2 minutes after failed
        private int Interval => (int) TimeSpan.FromMinutes(_config.DataCollectionMinutes).TotalMilliseconds; 
        private Timer _timer;
        private State _state;
        private List<Server> _servers;
        private StatisticsStrategyConfiguration _config;

        public static string AvailabilityStatisticsFile;

        //static constructor to initialize every public static fields before refereced
        static AvailabilityStatistics()
        {
            var temppath = Utils.GetTempPath();
            AvailabilityStatisticsFile = Path.Combine(temppath, StatisticsFilesName);
        }

        public AvailabilityStatistics(Configuration config, StatisticsStrategyConfiguration statisticsConfig)
        {
            UpdateConfiguration(config, statisticsConfig);
        }

        public bool Set(StatisticsStrategyConfiguration config)
        {
            _config = config;
            try
            {
                if (config.StatisticsEnabled)
                {
                    if (_timer?.Change(DelayBeforeStart, Interval) == null)
                    {
                        _state = new State();
                        _timer = new Timer(Run, _state, DelayBeforeStart, Interval);
                    }
                }
                else
                {
                    _timer?.Dispose();
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                return false;
            }
        }

        //hardcode
        //TODO: backup reliable isp&geolocation provider or a local database is required
        public static async Task<DataList> GetGeolocationAndIsp()
        {
            Logging.Debug("Retrive information of geolocation and isp");
            const string API = "http://ip-api.com/json";
            const string alternativeAPI = "http://www.telize.com/geoip"; //must be comptible with current API
            var result = await GetInfoFromAPI(API);
            if (result != null) return result;
            result = await GetInfoFromAPI(alternativeAPI);
            if (result != null) return result;
            return new DataList
            {
                new DataUnit(State.Geolocation, State.Unknown),
                new DataUnit(State.ISP, State.Unknown)
            };
        }

        private static async Task<DataList> GetInfoFromAPI(string API)
        {
            string jsonString;
            try
            {
                jsonString = await new HttpClient().GetStringAsync(API);
            }
            catch (HttpRequestException e)
            {
                Logging.LogUsefulException(e);
                return null;
            }
            dynamic obj;
            if (!SimpleJson.SimpleJson.TryDeserializeObject(jsonString, out obj)) return null;
            string country = obj["country"];
            string city = obj["city"];
            string isp = obj["isp"];
            if (country == null || city == null || isp == null) return null;
            return new DataList {
                new DataUnit(State.Geolocation, $"\"{country} {city}\""),
                new DataUnit(State.ISP, $"\"{isp}\"")
            };
        }

        private async Task<List<DataList>> ICMPTest(Server server)
        {
            Logging.Debug("Ping " + server.FriendlyName());
            if (server.server == "") return null;
            var IP = Dns.GetHostAddresses(server.server).First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            var ping = new Ping();
            var ret = new List<DataList>();
            foreach (
                var timestamp in Enumerable.Range(0, Repeat).Select(_ => DateTime.Now.ToString(DateTimePattern)))
            {
                //ICMP echo. we can also set options and special bytes
                try
                {
                    var reply = await ping.SendTaskAsync(IP, Timeout);
                    ret.Add(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Timestamp", timestamp),
                        new KeyValuePair<string, string>("Server", server.FriendlyName()),
                        new KeyValuePair<string, string>("Status", reply?.Status.ToString()),
                        new KeyValuePair<string, string>("RoundtripTime", reply?.RoundtripTime.ToString())
                        //new KeyValuePair<string, string>("data", reply.Buffer.ToString()); // The data of reply
                    });
                    Thread.Sleep(Timeout + new Random().Next() % Timeout);
                    //Do ICMPTest in a random frequency
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An exception occured when eveluating {server.FriendlyName()}");
                    Logging.LogUsefulException(e);
                }
            }
            return ret;
        }

        private void Run(object obj)
        {
            LoadRawStatistics();
            FilterRawStatistics();
            evaluate();
        }

        private async void evaluate()
        {
            var geolocationAndIsp = GetGeolocationAndIsp();
            foreach (var dataLists in await TaskEx.WhenAll(_servers.Select(ICMPTest)))
            {
                if (dataLists == null) continue;
                foreach (var dataList in dataLists.Where(dataList => dataList != null))
                {
                    await geolocationAndIsp;
                    Append(dataList, geolocationAndIsp.Result);
                }
            }
        }

        private static void Append(DataList dataList, IEnumerable<DataUnit> extra)
        {
            var data = dataList.Concat(extra);
            var dataLine = string.Join(Delimiter, data.Select(kv => kv.Value).ToArray());
            string[] lines;
            if (!File.Exists(AvailabilityStatisticsFile))
            {
                var headerLine = string.Join(Delimiter, data.Select(kv => kv.Key).ToArray());
                lines = new[] {headerLine, dataLine};
            }
            else
            {
                lines = new[] {dataLine};
            }
            try
            {
                File.AppendAllLines(AvailabilityStatisticsFile, lines);
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        internal void UpdateConfiguration(Configuration config, StatisticsStrategyConfiguration statisticsConfig)
        {
            Set(statisticsConfig);
            _servers = config.configs;
        }

        private async void FilterRawStatistics()
        {
            if (RawStatistics == null) return;
            if (FilteredStatistics == null)
            {
                FilteredStatistics = new Statistics();
            }
            foreach (IEnumerable<RawStatisticsData> rawData in RawStatistics.Values)
            {
                var filteredData = rawData;
                if (_config.ByIsp)
                {
                    var current = await GetGeolocationAndIsp();
                    filteredData =
                        filteredData.Where(
                            data =>
                                data.Geolocation == current[0].Value ||
                                data.Geolocation == State.Unknown);
                    filteredData =
                        filteredData.Where(
                            data => data.ISP == current[1].Value || data.ISP == State.Unknown);
                    if (filteredData.LongCount() == 0) return;
                }
                if (_config.ByHourOfDay)
                {
                    var currentHour = DateTime.Now.Hour;
                    filteredData = filteredData.Where(data =>
                        data.Timestamp != UnknownDateTime && data.Timestamp.Hour.Equals(currentHour)
                    );
                    if (filteredData.LongCount() == 0) return;
                }
                var dataList = filteredData as List<RawStatisticsData> ?? filteredData.ToList();
                var serverName = dataList[0].ServerName;
                FilteredStatistics[serverName] = dataList;
            }
        }

        private void LoadRawStatistics()
        {
            try
            {
                var path = AvailabilityStatisticsFile;
                Logging.Debug($"loading statistics from {path}");
                if (!File.Exists(path))
                {
                    Console.WriteLine($"statistics file does not exist, try to reload {RetryInterval/60/1000} minutes later");
                    _timer.Change(RetryInterval, Interval);
                    return;
                }
                RawStatistics = (from l in File.ReadAllLines(path)
                    .Skip(1)
                    let strings = l.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
                    let rawData = new RawStatisticsData
                    {
                        Timestamp = ParseExactOrUnknown(strings[0]),
                        ServerName = strings[1],
                        ICMPStatus = strings[2],
                        RoundtripTime = int.Parse(strings[3]),
                        Geolocation = 5 > strings.Length ?
                        null 
                        : strings[4],
                        ISP = 6 > strings.Length ? null : strings[5]
                    }
                    group rawData by rawData.ServerName into server
                    select new
                    {
                        ServerName = server.Key,
                        data = server.ToList()
                    }).ToDictionary(server => server.ServerName, server=> server.data);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private DateTime ParseExactOrUnknown(string str)
        {
            DateTime dateTime;
            return !DateTime.TryParseExact(str, DateTimePattern, null, DateTimeStyles.None, out dateTime) ? UnknownDateTime : dateTime;
        }

        public class State
        {
            public DataList dataList = new DataList();
            public const string Geolocation = "Geolocation";
            public const string ISP = "ISP";
            public const string Unknown = "Unknown";
        }

        public class RawStatisticsData
        {
            public DateTime Timestamp;
            public string ServerName;
            public string ICMPStatus;
            public int RoundtripTime;
            public string Geolocation;
            public string ISP ;
        }

        public class StatisticsData
        {
            public float PackageLoss;
            public int AverageResponse;
            public int MinResponse;
            public int MaxResponse;
        }

    }
}
