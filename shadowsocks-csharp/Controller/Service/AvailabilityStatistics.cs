using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using SimpleJson;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Timer = System.Threading.Timer;

namespace Shadowsocks.Controller
{
    using DataUnit = KeyValuePair<string, string>;
    using DataList = List<KeyValuePair<string, string>>;

    internal class AvailabilityStatistics
    {
        public static readonly string DateTimePattern = "yyyy-MM-dd HH:mm:ss";
        private const string StatisticsFilesName = "shadowsocks.availability.csv";
        private const string Delimiter = ",";
        private const int Timeout = 500;
        private const int DelayBeforeStart = 1000;
        private int _repeat => _config.RepeatTimesNum;
        private int _interval => (int) TimeSpan.FromMinutes(_config.DataCollectionMinutes).TotalMilliseconds; 
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
                    if (_timer?.Change(DelayBeforeStart, _interval) == null)
                    {
                        _state = new State();
                        _timer = new Timer(Evaluate, _state, DelayBeforeStart, _interval);
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
            const string api = "http://ip-api.com/json";
            var ret = new DataList
            {
                new DataUnit(State.Geolocation, State.Unknown),
                new DataUnit(State.ISP, State.Unknown),
            };
            string jsonString;
            try
            {
                jsonString = await new HttpClient().GetStringAsync(api);
            }
            catch (HttpRequestException e)
            {
                Logging.LogUsefulException(e);
                return ret;
            }
            dynamic obj;
            if (!global::SimpleJson.SimpleJson.TryDeserializeObject(jsonString, out obj)) return ret;
            string country = obj["country"];
            string city = obj["city"];
            string isp = obj["isp"];
            string regionName = obj["regionName"];
            if (country == null || city == null || isp == null || regionName == null) return ret;
            ret[0] = new DataUnit(State.Geolocation, $"{country} | {regionName} | {city}");
            ret[1] = new DataUnit(State.ISP, isp);
            return ret;
        }

        private async Task<List<DataList>> ICMPTest(Server server)
        {
            Logging.Debug("Ping " + server.FriendlyName());
            if (server.server == "") return null;
            var IP = Dns.GetHostAddresses(server.server).First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            var ping = new Ping();
            var ret = new List<DataList>();
            foreach (
                var timestamp in Enumerable.Range(0, _repeat).Select(_ => DateTime.Now.ToString(DateTimePattern)))
            {
                //ICMP echo. we can also set options and special bytes
                try
                {
                    var reply = ping.Send(IP, Timeout);
                    ret.Add(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Timestamp", timestamp),
                        new KeyValuePair<string, string>("Server", server.FriendlyName()),
                        new KeyValuePair<string, string>("Status", reply?.Status.ToString()),
                        new KeyValuePair<string, string>("RoundtripTime", reply?.RoundtripTime.ToString())
                        //new KeyValuePair<string, string>("data", reply.Buffer.ToString()); // The data of reply
                    });
                    Thread.Sleep(new Random().Next() % Timeout);
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

        private async void Evaluate(object obj)
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

        public class State
        {
            public DataList dataList = new DataList();
            public const string Geolocation = "Geolocation";
            public const string ISP = "ISP";
            public const string Unknown = "Unknown";
        }
    }
}
