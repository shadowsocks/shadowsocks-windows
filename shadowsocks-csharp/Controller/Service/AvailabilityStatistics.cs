using System;
using System.Collections;
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    using DataUnit = KeyValuePair<string, string>;
    using DataList = List<KeyValuePair<string, string>>;

    using Statistics = Dictionary<string, List<AvailabilityStatistics.RawStatisticsData>>;

    //TODO: change to singleton
    public class AvailabilityStatistics
    {
        public const string DateTimePattern = "yyyy-MM-dd HH:mm:ss";
        private const string StatisticsFilesName = "shadowsocks.availability.csv";
        private const string Delimiter = ",";
        private const int Timeout = 500;
        private readonly TimeSpan DelayBeforeStart = TimeSpan.FromSeconds(1);
        public Statistics RawStatistics { get; private set; }
        public Statistics FilteredStatistics { get; private set; }
        public static readonly DateTime UnknownDateTime = new DateTime(1970, 1, 1);
        private int Repeat => _config.RepeatTimesNum;
        private readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(2); //retry 2 minutes after failed
        private TimeSpan Interval => TimeSpan.FromMinutes(_config.DataCollectionMinutes);
        private Timer _timer;
        private Timer _speedMonior;
        private State _state;
        private List<Server> _servers;
        private StatisticsStrategyConfiguration _config;

        private const string Empty = "";

        public static string AvailabilityStatisticsFile;
        //speed in KiB/s
        private int _inboundSpeed = 0;
        private int _outboundSpeed = 0;
        private int? _latency = 0;
        private Server _currentServer;
        private Configuration _globalConfig;
        private readonly ShadowsocksController _controller;
        private long _lastInboundCounter = 0;
        private long _lastOutboundCounter = 0;
        private readonly TimeSpan MonitorInterval = TimeSpan.FromSeconds(1);

        //static constructor to initialize every public static fields before refereced
        static AvailabilityStatistics()
        {
            AvailabilityStatisticsFile = Utils.GetTempPath(StatisticsFilesName);
        }

        public AvailabilityStatistics(ShadowsocksController controller)
        {
            _controller = controller;
            _globalConfig = controller.GetCurrentConfiguration();
            UpdateConfiguration(_globalConfig, controller.StatisticsConfiguration);
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
                    _speedMonior?.Dispose();
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                return false;
            }
        }

        private void UpdateSpeed(object state)
        {
            var bytes = _controller.inboundCounter - _lastInboundCounter;
            _lastInboundCounter = _controller.inboundCounter;
            var inboundSpeed = GetSpeedInKiBPerSecond(bytes ,MonitorInterval.TotalSeconds);

            bytes = _controller.outboundCounter - _lastOutboundCounter;
            _lastOutboundCounter = _controller.outboundCounter;
            var outboundSpeed = GetSpeedInKiBPerSecond(bytes, MonitorInterval.TotalSeconds);

            if (inboundSpeed > _inboundSpeed)
            {
                _inboundSpeed = inboundSpeed;
            }
            if (outboundSpeed > _outboundSpeed)
            {
                _outboundSpeed = outboundSpeed;
            }
            Logging.Debug($"{_currentServer.FriendlyName()}: current/max inbound {inboundSpeed}/{_inboundSpeed} KiB/s, current/max outbound {outboundSpeed}/{_outboundSpeed} KiB/s");
        }

        private async Task<List<DataList>> ICMPTest(Server server)
        {
            Logging.Debug("Ping " + server.FriendlyName());
            if (server.server == "") return null;
            var ret = new List<DataList>();
            try {
                var IP = Dns.GetHostAddresses(server.server).First(ip => (ip.AddressFamily == AddressFamily.InterNetwork || ip.AddressFamily == AddressFamily.InterNetworkV6));
                var ping = new Ping();

                foreach (var timestamp in Enumerable.Range(0, Repeat).Select(_ => DateTime.Now.ToString(DateTimePattern)))
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
                        new KeyValuePair<string, string>("RoundtripTime", reply?.RoundtripTime.ToString()),
                        new KeyValuePair<string, string>("Latency", GetRecentLatency(server)),
                        new KeyValuePair<string, string>("InboundSpeed", GetRecentInboundSpeed(server)),
                        new KeyValuePair<string, string>("OutboundSpeed", GetRecentOutboundSpeed(server))
                        //new KeyValuePair<string, string>("data", reply.Buffer.ToString()); // The data of reply
                    });
                        Thread.Sleep(Timeout + new Random().Next() % Timeout);
                        //Do ICMPTest in a random frequency
                    }
                    catch (Exception e)
                    {
                        Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                        Logging.LogUsefulException(e);
                    }
                }
            }catch(Exception e)
            {
                Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                Logging.LogUsefulException(e);
            }
            return ret;
        }


        private string GetRecentOutboundSpeed(Server server)
        {
            if (server != _currentServer) return Empty;
            return _outboundSpeed.ToString();
        }

        private string GetRecentInboundSpeed(Server server)
        {
            if (server != _currentServer) return Empty;
            return _inboundSpeed.ToString();
        }

        private string GetRecentLatency(Server server)
        {
            if (server != _currentServer) return Empty;
            return _latency == null ? Empty : _latency.ToString();
        }

        private void ResetSpeed()
        {
            _currentServer = _globalConfig.GetCurrentServer();
            _latency = null;
            _inboundSpeed = 0;
            _outboundSpeed = 0;
        }

        private void Run(object obj)
        {
            if (_speedMonior?.Change(DelayBeforeStart, MonitorInterval) == null)
            {
                _speedMonior = new Timer(UpdateSpeed, null, DelayBeforeStart, MonitorInterval);
            }
            LoadRawStatistics();
            FilterRawStatistics();
            evaluate();
            ResetSpeed();
        }

        private async void evaluate()
        {
            foreach (var dataLists in await TaskEx.WhenAll(_servers.Select(ICMPTest)))
            {
                if (dataLists == null) continue;
                foreach (var dataList in dataLists.Where(dataList => dataList != null))
                {
                    Append(dataList, Enumerable.Empty<DataUnit>());
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
            ResetSpeed();
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
                    try {
                        using (FileStream fs = File.Create(path))
                        {
                            //do nothing
                        }
                    }catch(Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                    if (!File.Exists(path)) {
                        Console.WriteLine($"statistics file does not exist, try to reload {RetryInterval.TotalMinutes} minutes later");
                        _timer.Change(RetryInterval, Interval);
                        return;
                    }
                }
                RawStatistics = (from l in File.ReadAllLines(path).Skip(1)
                                 let strings = l.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                                 let rawData = new RawStatisticsData
                                 {
                                     Timestamp = ParseExactOrUnknown(strings[0]),
                                     ServerName = strings[1],
                                     ICMPStatus = strings[2],
                                     RoundtripTime = int.Parse(strings[3])
                                 }
                                 group rawData by rawData.ServerName into server
                                 select new
                                 {
                                     ServerName = server.Key,
                                     data = server.ToList()
                                 }).ToDictionary(server => server.ServerName, server => server.data);
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
            public const string Unknown = "Unknown";
        }

        //TODO: redesign model
        public class RawStatisticsData
        {
            public DateTime Timestamp;
            public string ServerName;
            public string ICMPStatus;
            public int RoundtripTime;
        }

        public class StatisticsData
        {
            public float PackageLoss;
            public int AverageResponse;
            public int MinResponse;
            public int MaxResponse;
        }

        public void UpdateLatency(int latency)
        {
            _latency = latency;
        }

        private static int GetSpeedInKiBPerSecond(long bytes, double seconds)
        {
            var result = (int) (bytes / seconds) / 1024;
            return result;
        }
    }
}
