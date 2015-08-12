using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    class AvailabilityStatistics
    {
        private const string StatisticsFilesName = "shadowsocks.availability.csv";
        private const string Delimiter = ",";
        private const int Timeout = 500;
        private const int Repeat = 4; //repeat times every evaluation
        private const int Interval = 10*60*1000; //evaluate proxies every 15 minutes
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
                if (enabled)
                {
                    if (_timer?.Change(0, Interval) != null) return true;
                    _state = new State();
                    _timer = new Timer(Evaluate, _state, 0, Interval);
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

        private void Evaluate(object obj)
        {
            var ping = new Ping();
            var state = (State) obj;
            foreach (var server in _servers)
            {
                Logging.Debug("eveluating " + server.FriendlyName());
                foreach (var _ in Enumerable.Range(0, Repeat))
                {
                    //TODO: do simple analyze of data to provide friendly message, like package loss.
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    //ICMP echo. we can also set options and special bytes
                    //seems no need to use SendPingAsync：
                    var reply = ping.Send(server.server, Timeout);
                    state.Data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Timestamp", timestamp),
                        new KeyValuePair<string, string>("Server", server.FriendlyName()),
                        new KeyValuePair<string, string>("Status", reply?.Status.ToString()),
                        new KeyValuePair<string, string>("RoundtripTime", reply?.RoundtripTime.ToString())
                    };
                    //state.data.Add(new KeyValuePair<string, string>("data", reply.Buffer.ToString())); // The data of reply
                    Append(state.Data);
                }
            }
        }

        private static void Append(List<KeyValuePair<string, string>> data)
        {
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
            public List<KeyValuePair<string, string>> Data = new List<KeyValuePair<string, string>>();
        }
    }
}
