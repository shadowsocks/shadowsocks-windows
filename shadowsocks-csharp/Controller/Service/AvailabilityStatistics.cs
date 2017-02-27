using System;
using System.Collections.Concurrent;
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
        private const string StatisticsFilesName = "shadowsocks.availability.json";
        public static string AvailabilityStatisticsFile;
        //static constructor to initialize every public static fields before refereced
        static AvailabilityStatistics()
        {
            AvailabilityStatisticsFile = Utils.GetTempPath(StatisticsFilesName);
        }

        //arguments for ICMP tests
        private int Repeat => Config.RepeatTimesNum;
        public const int TimeoutMilliseconds = 500;

        //records cache for current server in {_monitorInterval} minutes
        private readonly ConcurrentDictionary<string, List<int>> _latencyRecords = new ConcurrentDictionary<string, List<int>>();
        //speed in KiB/s
        private readonly ConcurrentDictionary<string, List<int>> _inboundSpeedRecords = new ConcurrentDictionary<string, List<int>>();
        private readonly ConcurrentDictionary<string, List<int>> _outboundSpeedRecords = new ConcurrentDictionary<string, List<int>>();
        private readonly ConcurrentDictionary<string, InOutBoundRecord> _inOutBoundRecords = new ConcurrentDictionary<string, InOutBoundRecord>();

        private class InOutBoundRecord
        {
            private long _inbound;
            private long _lastInbound;
            private long _outbound;
            private long _lastOutbound;

            public void UpdateInbound(long delta)
            {
                Interlocked.Add(ref _inbound, delta);
            }

            public void UpdateOutbound(long delta)
            {
                Interlocked.Add(ref _outbound, delta);
            }

            public void GetDelta(out long inboundDelta, out long outboundDelta)
            {
                var i = Interlocked.Read(ref _inbound);
                var il = Interlocked.Exchange(ref _lastInbound, i);
                inboundDelta = i - il;


                var o = Interlocked.Read(ref _outbound);
                var ol = Interlocked.Exchange(ref _lastOutbound, o);
                outboundDelta = o - ol;
            }
        }

        //tasks
        private readonly TimeSpan _delayBeforeStart = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(2);
        private Timer _recorder; //analyze and save cached records to RawStatistics and filter records
        private TimeSpan RecordingInterval => TimeSpan.FromMinutes(Config.DataCollectionMinutes);
        private Timer _speedMonior;
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(1);
        //private Timer _writer; //write RawStatistics to file
        //private readonly TimeSpan _writingInterval = TimeSpan.FromMinutes(1);

        private ShadowsocksController _controller;
        private StatisticsStrategyConfiguration Config => _controller.StatisticsConfiguration;

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
                }
                else
                {
                    _recorder?.Dispose();
                    _speedMonior?.Dispose();
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
            foreach (var kv in _inOutBoundRecords)
            {
                var id = kv.Key;
                var record = kv.Value;

                long inboundDelta, outboundDelta;

                record.GetDelta(out inboundDelta, out outboundDelta);

                var inboundSpeed = GetSpeedInKiBPerSecond(inboundDelta, _monitorInterval.TotalSeconds);
                var outboundSpeed = GetSpeedInKiBPerSecond(outboundDelta, _monitorInterval.TotalSeconds);

                var inR = _inboundSpeedRecords.GetOrAdd(id, (k) => new List<int>());
                var outR = _outboundSpeedRecords.GetOrAdd(id, (k) => new List<int>());

                inR.Add(inboundSpeed);
                outR.Add(outboundSpeed);

                Logging.Debug(
                    $"{id}: current/max inbound {inboundSpeed}/{inR.Max()} KiB/s, current/max outbound {outboundSpeed}/{outR.Max()} KiB/s");
            }
        }

        private void Reset()
        {
            _inboundSpeedRecords.Clear();
            _outboundSpeedRecords.Clear();
            _latencyRecords.Clear();
        }

        private void Run(object _)
        {
            UpdateRecords();
            Reset();
        }

        private void UpdateRecords()
        {
            var records = new Dictionary<string, StatisticsRecord>();
            UpdateRecordsState state = new UpdateRecordsState();
            state.counter = _controller.GetCurrentConfiguration().configs.Count;
            foreach (var server in _controller.GetCurrentConfiguration().configs)
            {
                var id = server.Identifier();
                List<int> inboundSpeedRecords = null;
                List<int> outboundSpeedRecords = null;
                List<int> latencyRecords = null;
                _inboundSpeedRecords.TryGetValue(id, out inboundSpeedRecords);
                _outboundSpeedRecords.TryGetValue(id, out outboundSpeedRecords);
                _latencyRecords.TryGetValue(id, out latencyRecords);
                StatisticsRecord record = new StatisticsRecord(id, inboundSpeedRecords, outboundSpeedRecords, latencyRecords);
                /* duplicate server identifier */
                if (records.ContainsKey(id))
                    records[id] = record;
                else
                    records.Add(id, record);
                if (Config.Ping)
                {
                    MyPing ping = new MyPing(server, Repeat);
                    ping.Completed += ping_Completed;
                    ping.Start(new PingState { state = state, record = record });
                }
                else if (!record.IsEmptyData())
                {
                    AppendRecord(id, record);
                }
            }

            if (!Config.Ping)
            {
                Save();
                FilterRawStatistics();
            }
        }

        private void ping_Completed(object sender, MyPing.CompletedEventArgs e)
        {
            PingState pingState = (PingState)e.UserState;
            UpdateRecordsState state = pingState.state;
            Server server = e.Server;
            StatisticsRecord record = pingState.record;
            record.SetResponse(e.RoundtripTime);
            if (!record.IsEmptyData())
            {
                AppendRecord(server.Identifier(), record);
            }
            Logging.Debug($"Ping {server.FriendlyName()} {e.RoundtripTime.Count} times, {(100 - record.PackageLoss * 100)}% packages loss, min {record.MinResponse} ms, max {record.MaxResponse} ms, avg {record.AverageResponse} ms");
            if (Interlocked.Decrement(ref state.counter) == 0)
            {
                Save();
                FilterRawStatistics();
            }
        }

        private void AppendRecord(string serverIdentifier, StatisticsRecord record)
        {
            try
            {
                List<StatisticsRecord> records;
                lock (RawStatistics)
                {
                    if (!RawStatistics.TryGetValue(serverIdentifier, out records))
                    {
                        records = new List<StatisticsRecord>();
                        RawStatistics[serverIdentifier] = records;
                    }
                }
                records.Add(record);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private void Save()
        {
            Logging.Debug($"save statistics to {AvailabilityStatisticsFile}");
            if (RawStatistics.Count == 0)
            {
                return;
            }
            try
            {
                string content;
#if DEBUG
                content = JsonConvert.SerializeObject(RawStatistics, Formatting.Indented);
#else
                content = JsonConvert.SerializeObject(RawStatistics, Formatting.None);
#endif
                File.WriteAllText(AvailabilityStatisticsFile, content);
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private bool IsValidRecord(StatisticsRecord record)
        {
            if (Config.ByHourOfDay)
            {
                if (!record.Timestamp.Hour.Equals(DateTime.Now.Hour)) return false;
            }
            return true;
        }

        private void FilterRawStatistics()
        {
            try
            {
                Logging.Debug("filter raw statistics");
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
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
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

        private static int GetSpeedInKiBPerSecond(long bytes, double seconds)
        {
            var result = (int)(bytes / seconds) / 1024;
            return result;
        }

        public void Dispose()
        {
            _recorder.Dispose();
            _speedMonior.Dispose();
        }

        public void UpdateLatency(Server server, int latency)
        {
            _latencyRecords.GetOrAdd(server.Identifier(), (k) =>
            {
                List<int> records = new List<int>();
                records.Add(latency);
                return records;
            });
        }

        public void UpdateInboundCounter(Server server, long n)
        {
            _inOutBoundRecords.AddOrUpdate(server.Identifier(), (k) =>
            {
                var r = new InOutBoundRecord();
                r.UpdateInbound(n);

                return r;
            }, (k, v) =>
            {
                v.UpdateInbound(n);
                return v;
            });
        }

        public void UpdateOutboundCounter(Server server, long n)
        {
            _inOutBoundRecords.AddOrUpdate(server.Identifier(), (k) =>
            {
                var r = new InOutBoundRecord();
                r.UpdateOutbound(n);

                return r;
            }, (k, v) =>
            {
                v.UpdateOutbound(n);
                return v;
            });
        }

        class UpdateRecordsState
        {
            public int counter;
        }

        class PingState
        {
            public UpdateRecordsState state;
            public StatisticsRecord record;
        }

        class MyPing
        {
            //arguments for ICMP tests
            public const int TimeoutMilliseconds = 500;

            public EventHandler<CompletedEventArgs> Completed;
            private Server server;

            private int repeat;
            private IPAddress ip;
            private Ping ping;
            private List<int?> RoundtripTime;

            public MyPing(Server server, int repeat)
            {
                this.server = server;
                this.repeat = repeat;
                RoundtripTime = new List<int?>(repeat);
                ping = new Ping();
                ping.PingCompleted += Ping_PingCompleted;
            }

            public void Start(object userstate)
            {
                if (server.server == "")
                {
                    FireCompleted(new Exception("Invalid Server"), userstate);
                    return;
                }
                new Task(() => ICMPTest(0, userstate)).Start();
            }

            private void ICMPTest(int delay, object userstate)
            {
                try
                {
                    Logging.Debug($"Ping {server.FriendlyName()}");
                    if (ip == null)
                    {
                        ip = Dns.GetHostAddresses(server.server)
                                .First(
                                    ip =>
                                        ip.AddressFamily == AddressFamily.InterNetwork ||
                                        ip.AddressFamily == AddressFamily.InterNetworkV6);
                    }
                    repeat--;
                    if (delay > 0)
                        Thread.Sleep(delay);
                    ping.SendAsync(ip, TimeoutMilliseconds, userstate);
                }
                catch (Exception e)
                {
                    Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                    Logging.LogUsefulException(e);
                    FireCompleted(e, userstate);
                }
            }

            private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
            {
                try
                {
                    if (e.Reply.Status == IPStatus.Success)
                    {
                        Logging.Debug($"Ping {server.FriendlyName()} {e.Reply.RoundtripTime} ms");
                        RoundtripTime.Add((int?)e.Reply.RoundtripTime);
                    }
                    else
                    {
                        Logging.Debug($"Ping {server.FriendlyName()} timeout");
                        RoundtripTime.Add(null);
                    }
                    TestNext(e.UserState);
                }
                catch (Exception ex)
                {
                    Logging.Error($"An exception occured while eveluating {server.FriendlyName()}");
                    Logging.LogUsefulException(ex);
                    FireCompleted(ex, e.UserState);
                }
            }

            private void TestNext(object userstate)
            {
                if (repeat > 0)
                {
                    //Do ICMPTest in a random frequency
                    int delay = TimeoutMilliseconds + new Random().Next() % TimeoutMilliseconds;
                    new Task(() => ICMPTest(delay, userstate)).Start();
                }
                else
                {
                    FireCompleted(null, userstate);
                }
            }

            private void FireCompleted(Exception error, object userstate)
            {
                Completed?.Invoke(this, new CompletedEventArgs
                {
                    Error = error,
                    Server = server,
                    RoundtripTime = RoundtripTime,
                    UserState = userstate
                });
            }

            public class CompletedEventArgs : EventArgs
            {
                public Exception Error;
                public Server Server;
                public List<int?> RoundtripTime;
                public object UserState;
            }
        }

    }
}
