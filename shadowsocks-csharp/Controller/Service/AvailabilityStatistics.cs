using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    using Statistics = Dictionary<string, List<StatisticsRecord>>;

    public sealed class AvailabilityStatistics : IDisposable
    {
        public const string DateTimePattern = "yyyy-MM-dd HH:mm:ss";
        private const string StatisticsFilesName = "shadowsocks.availability.json";
        private const int TimeoutMilliseconds = 500;

        public static readonly DateTime UnknownDateTime = new DateTime(1970, 1, 1);

        public static string AvailabilityStatisticsFile;
        private readonly TimeSpan _delayBeforeStart = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(2); //retry 2 minutes after failed
        private readonly TimeSpan _writingInterval = TimeSpan.FromMinutes(1);
        private StatisticsStrategyConfiguration _config;
        private ShadowsocksController _controller;
        private Server _currentServer;
        //speed in KiB/s
        private List<int> _inboundSpeedRecords;
        private long _lastInboundCounter;
        private long _lastOutboundCounter;
        private List<int> _latencyRecords;
        private List<int> _outboundSpeedRecords;
        private Timer _recorder;
        private List<Server> _servers;
        private Timer _speedMonior;
        private Timer _writer;

        //static constructor to initialize every public static fields before refereced
        static AvailabilityStatistics()
        {
            AvailabilityStatisticsFile = Utils.GetTempPath(StatisticsFilesName);
        }

        private AvailabilityStatistics()
        {
            RawStatistics = new Statistics();
        }

        // Static Singleton Initialization
        public static AvailabilityStatistics Instance { get; } = new AvailabilityStatistics();
        public Statistics RawStatistics { get; private set; }
        public Statistics FilteredStatistics { get; private set; }
        private int Repeat => _config.RepeatTimesNum;
        private TimeSpan RecordingInterval => TimeSpan.FromMinutes(_config.DataCollectionMinutes);

        public bool Set(StatisticsStrategyConfiguration config)
        {
            _config = config;
            try
            {
                if (config.StatisticsEnabled)
                {
                    if (_recorder?.Change(_delayBeforeStart, RecordingInterval) == null)
                    {
                        _recorder = new Timer(Run, null, _delayBeforeStart, RecordingInterval);
                    }
                    LoadRawStatistics();
                    if (_speedMonior?.Change(_delayBeforeStart, _monitorInterval) == null)
                    {
                        _speedMonior = new Timer(UpdateSpeed, null, _delayBeforeStart, _monitorInterval);
                    }
                    if (_writer?.Change(_delayBeforeStart, RecordingInterval) == null)
                    {
                        _writer = new Timer(Save, null, _delayBeforeStart, RecordingInterval);
                    }
                }
                else
                {
                    _recorder?.Dispose();
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
            var inboundSpeed = GetSpeedInKiBPerSecond(bytes, _monitorInterval.TotalSeconds);
            _inboundSpeedRecords.Add(inboundSpeed);

            bytes = _controller.outboundCounter - _lastOutboundCounter;
            _lastOutboundCounter = _controller.outboundCounter;
            var outboundSpeed = GetSpeedInKiBPerSecond(bytes, _monitorInterval.TotalSeconds);
            _outboundSpeedRecords.Add(outboundSpeed);

            Logging.Debug(
                $"{_currentServer.FriendlyName()}: current/max inbound {inboundSpeed}/{_inboundSpeedRecords.Max()} KiB/s, current/max outbound {outboundSpeed}/{_outboundSpeedRecords.Max()} KiB/s");
        }

        private async Task<ICMPResult> ICMPTest(Server server)
        {
            Logging.Debug("Ping " + server.FriendlyName());
            if (server.server == "") return null;
            var result = new ICMPResult(server);
            try
            {
                var IP =
                    Dns.GetHostAddresses(server.server)
                        .First(
                            ip =>
                                ip.AddressFamily == AddressFamily.InterNetwork ||
                                ip.AddressFamily == AddressFamily.InterNetworkV6);
                var ping = new Ping();

                foreach (var _ in Enumerable.Range(0, Repeat))
                {
                    try
                    {
                        var reply = await ping.SendTaskAsync(IP, TimeoutMilliseconds);
                        if (!reply.Status.Equals(IPStatus.Success))
                        {
                            result.RoundtripTime.Add((int?) reply.RoundtripTime);
                        }
                        else
                        {
                            result.RoundtripTime.Add(null);
                        }

                        //Do ICMPTest in a random frequency
                        Thread.Sleep(TimeoutMilliseconds + new Random().Next()%TimeoutMilliseconds);
                    }
                    catch (Exception e)
                    {
                        Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                        Logging.LogUsefulException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                Logging.LogUsefulException(e);
            }
            return result;
        }

        private void Reset()
        {
            _inboundSpeedRecords = new List<int>();
            _outboundSpeedRecords = new List<int>();
            _latencyRecords = new List<int>();
        }

        private void Run(object _)
        {
            AppendRecord();
            Reset();
            FilterRawStatistics();
        }

        private async void AppendRecord()
        {
            //todo: option for icmp test
            var icmpResults = TaskEx.WhenAll(_servers.Select(ICMPTest));

            var currentServerRecord = new StatisticsRecord(_currentServer.Identifier(),
                _inboundSpeedRecords, _outboundSpeedRecords, _latencyRecords);

            foreach (var result in (await icmpResults).Where(result => result != null))
            {
                List<StatisticsRecord> records;
                if (!RawStatistics.TryGetValue(result.Server.Identifier(), out records))
                {
                    records = new List<StatisticsRecord>();
                }

                if (result.Server.Equals(_currentServer))
                {
                    currentServerRecord.setResponse(result.RoundtripTime);
                    records.Add(currentServerRecord);
                }
                else
                {
                    records.Add(new StatisticsRecord(result.Server.Identifier(), result.RoundtripTime));
                }
                RawStatistics[result.Server.Identifier()] = records;
            }
        }

        private void Save(object _)
        {
            try
            {
                File.WriteAllText(AvailabilityStatisticsFile,
                    JsonConvert.SerializeObject(RawStatistics, Formatting.None));
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
                _writer.Change(_retryInterval, _writingInterval);
            }
        }

        /*
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
        */

        internal void UpdateConfiguration(ShadowsocksController controller)
        {
            _controller = controller;
            _currentServer = _controller.GetCurrentServer();
            Reset();
            Set(controller.StatisticsConfiguration);
            _servers = _controller.GetCurrentConfiguration().configs;
        }

        private bool IsValidRecord(StatisticsRecord record)
        {
            if (_config.ByHourOfDay)
            {
                var currentHour = DateTime.Now.Hour;
                if (record.Timestamp == UnknownDateTime) return false;
                if (!record.Timestamp.Hour.Equals(DateTime.Now.Hour)) return false;
            }
            return true;
        }

        private void FilterRawStatistics()
        {
            if (RawStatistics == null) return;
            if (FilteredStatistics == null)
            {
                FilteredStatistics = new Statistics();
            }

            foreach (var serverAndRecords in RawStatistics)
            {
                var server = serverAndRecords.Key;
                var filteredRecords = serverAndRecords.Value.FindAll(IsValidRecord);
                FilteredStatistics[server] = filteredRecords;
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
                    try
                    {
                        using (var fs = File.Create(path))
                        {
                            //do nothing
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                    if (!File.Exists(path))
                    {
                        Console.WriteLine(
                            $"statistics file does not exist, try to reload {_retryInterval.TotalMinutes} minutes later");
                        _recorder.Change(_retryInterval, RecordingInterval);
                        return;
                    }
                }
                RawStatistics = JsonConvert.DeserializeObject<Statistics>(File.ReadAllText(path)) ?? RawStatistics;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private DateTime ParseExactOrUnknown(string str)
        {
            DateTime dateTime;
            return !DateTime.TryParseExact(str, DateTimePattern, null, DateTimeStyles.None, out dateTime)
                ? UnknownDateTime
                : dateTime;
        }

        public void UpdateLatency(int latency)
        {
            _latencyRecords.Add(latency);
        }

        private static int GetSpeedInKiBPerSecond(long bytes, double seconds)
        {
            var result = (int) (bytes/seconds)/1024;
            return result;
        }

        private class ICMPResult
        {
            internal readonly List<int?> RoundtripTime = new List<int?>();
            internal readonly Server Server;

            internal ICMPResult(Server server)
            {
                Server = server;
            }
        }

        public void Dispose()
        {
            _recorder.Dispose();
            _writer.Dispose();
            _speedMonior.Dispose();
        }
    }
}