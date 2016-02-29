using System;
using System.Collections.Generic;
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
        public static readonly DateTime UnknownDateTime;
        private const string StatisticsFilesName = "shadowsocks.availability.json";
        public static string AvailabilityStatisticsFile;
        //static constructor to initialize every public static fields before refereced
        static AvailabilityStatistics()
        {
            AvailabilityStatisticsFile = Utils.GetTempPath(StatisticsFilesName);
            UnknownDateTime = new DateTime(1970, 1, 1);
        }

        //arguments for ICMP tests
        private int Repeat => Config.RepeatTimesNum;
        public const int TimeoutMilliseconds = 500;

        //records cache for current server in {_monitorInterval} minutes
        private List<int> _latencyRecords;
        //speed in KiB/s
        private long _lastInboundCounter;
        private List<int> _inboundSpeedRecords;
        private long _lastOutboundCounter;
        private List<int> _outboundSpeedRecords;

        //tasks
        private readonly TimeSpan _delayBeforeStart = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(2);
        private Timer _recorder; //analyze and save cached records to RawStatistics and filter records
        private TimeSpan RecordingInterval => TimeSpan.FromMinutes(Config.DataCollectionMinutes);
        private Timer _speedMonior;
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(1);
        private Timer _writer; //write RawStatistics to file
        private readonly TimeSpan _writingInterval = TimeSpan.FromMinutes(1);

        private ShadowsocksController _controller;
        private StatisticsStrategyConfiguration Config => _controller.StatisticsConfiguration;
        private Server CurrentServer => _controller.GetCurrentServer();

        // Static Singleton Initialization
        public static AvailabilityStatistics Instance { get; } = new AvailabilityStatistics();
        public Statistics RawStatistics { get; private set; }
        public Statistics FilteredStatistics { get; private set; }

        private AvailabilityStatistics()
        {
            RawStatistics = new Statistics();
        }

        internal void UpdateConfiguration(ShadowsocksController controller)
        {
            _controller = controller;
            Reset();
            try
            {
                if (Config.StatisticsEnabled)
                {
                    StartTimerWithoutState(ref _recorder, Run, RecordingInterval);
                    LoadRawStatistics();
                    StartTimerWithoutState(ref _speedMonior, UpdateSpeed, _monitorInterval);
                    StartTimerWithoutState(ref _writer, Save, _writingInterval);
                }
                else
                {
                    _recorder?.Dispose();
                    _speedMonior?.Dispose();
                    _writer?.Dispose();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private void StartTimerWithoutState(ref Timer timer, TimerCallback callback, TimeSpan interval)
        {
            if (timer?.Change(_delayBeforeStart, interval) == null)
            {
                timer = new Timer(callback, null, _delayBeforeStart, interval);
            }
        }

        private void UpdateSpeed(object _)
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
                $"{CurrentServer.FriendlyName()}: current/max inbound {inboundSpeed}/{_inboundSpeedRecords.Max()} KiB/s, current/max outbound {outboundSpeed}/{_outboundSpeedRecords.Max()} KiB/s");
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
                        if (reply.Status.Equals(IPStatus.Success))
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
            UpdateRecords();
            Reset();
            FilterRawStatistics();
        }

        private async void UpdateRecords()
        {
            var currentServerRecord = new StatisticsRecord(CurrentServer.Identifier(), _inboundSpeedRecords, _outboundSpeedRecords, _latencyRecords);

            if (!Config.Ping)
            {
                AppendRecord(CurrentServer, currentServerRecord);
                return;
            }

            var icmpResults = TaskEx.WhenAll(_controller.GetCurrentConfiguration().configs.Select(ICMPTest));

            foreach (var result in (await icmpResults).Where(result => result != null))
            {
                if (result.Server.Equals(CurrentServer))
                {
                    currentServerRecord.setResponse(result.RoundtripTime);
                    AppendRecord(CurrentServer, currentServerRecord);
                }
                else
                {
                    AppendRecord(result.Server, new StatisticsRecord(result.Server.Identifier(), result.RoundtripTime));
                }
            }
        }

        private void AppendRecord(Server server, StatisticsRecord record)
        {
            List<StatisticsRecord> records;
            if (!RawStatistics.TryGetValue(server.Identifier(), out records))
            {
                records = new List<StatisticsRecord>();
            }
            records.Add(record);
            RawStatistics[server.Identifier()] = records;
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

        private bool IsValidRecord(StatisticsRecord record)
        {
            if (Config.ByHourOfDay)
            {
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
                    using (File.Create(path))
                    {
                        //do nothing
                    }
                }
                var content = File.ReadAllText(path);
                RawStatistics = JsonConvert.DeserializeObject<Statistics>(content) ?? RawStatistics;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                Console.WriteLine($"failed to load statistics; try to reload {_retryInterval.TotalMinutes} minutes later");
                _recorder.Change(_retryInterval, RecordingInterval);
            }
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
