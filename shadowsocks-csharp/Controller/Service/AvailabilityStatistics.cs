using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using SimpleJson;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shadowsocks.Model;
using SimpleJson = SimpleJson.SimpleJson;
using Timer = System.Threading.Timer;

namespace Shadowsocks.Controller
{
    using DataUnit = KeyValuePair<string, string>;
    using DataList = List<KeyValuePair<string, string>>;
    class AvailabilityStatistics
    {
        private const string StatisticsFilesName = "shadowsocks.availability.csv";
        private const string Delimiter = ",";
        private const int Timeout = 500;
        private const int Repeat = 4; //repeat times every evaluation
        private const int Interval = 10*60*1000; //evaluate proxies every 15 minutes
        private const int delayBeforeStart = 1*1000; //delay 1 second before start
        private Timer _timer;
        private State _state;
        private List<Server> _servers;

        public static string AvailabilityStatisticsFile;

        //static constructor to initialize every public static fields before refereced
        static AvailabilityStatistics()
        {
            var temppath = Path.GetTempPath();
            AvailabilityStatisticsFile = Path.Combine(temppath, StatisticsFilesName);
        }

        public bool Set(bool enabled)
        {
            try
            {
                _timer?.Dispose();
                if (!enabled) return true;
                _state = new State();
                _timer = new Timer(Evaluate, _state, delayBeforeStart, Interval);
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
        private static async Task<DataList> getGeolocationAndISP()
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
            string regionName= obj["regionName"];
            if (country == null || city == null || isp == null || regionName == null) return ret;
            ret[0] = new DataUnit(State.Geolocation, $"{country} {regionName} {city}");
            ret[1] = new DataUnit(State.ISP, isp);
            return ret;
        }

        private static async Task<List<DataList>> ICMPTest(Server server)
        {
            Logging.Debug("eveluating " + server.FriendlyName());
            if (server.server == "") return null;
            var ping = new Ping();
            var ret = new List<DataList>();
            foreach (var timestamp in Enumerable.Range(0, Repeat).Select(_ => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                //ICMP echo. we can also set options and special bytes
                var reply = await ping.SendTaskAsync(server.server, Timeout);
                ret.Add(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Timestamp", timestamp),
                    new KeyValuePair<string, string>("Server", server.FriendlyName()),
                    new KeyValuePair<string, string>("Status", reply?.Status.ToString()),
                    new KeyValuePair<string, string>("RoundtripTime", reply?.RoundtripTime.ToString())
                    //new KeyValuePair<string, string>("data", reply.Buffer.ToString()); // The data of reply
                });
            }
            return ret;
        }

        private async void Evaluate(object obj)
        {
            var geolocationAndIsp = getGeolocationAndISP();
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
                lines = new[] { headerLine, dataLine };
            }
            else
            {
                lines = new[] { dataLine };
            }
            File.AppendAllLines(AvailabilityStatisticsFile, lines);
        }

        internal void UpdateConfiguration(Configuration config)
        {
            Set(config.availabilityStatistics);
            _servers = config.configs;
        }

        private class State
        {
            public DataList dataList = new DataList();
            public const string Geolocation = "Geolocation";
            public const string ISP = "ISP";
            public const string Unknown = "Unknown";
        }
    }
}
