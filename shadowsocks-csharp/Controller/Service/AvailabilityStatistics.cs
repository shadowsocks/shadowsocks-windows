using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Shadowsocks.Model;
using System.Reflection;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    class AvailabilityStatistics
    {
        private static readonly string StatisticsFilesName = "shadowsocks.availability.csv";
        private static readonly string Delimiter = ",";
        private static readonly int Timeout = 500;
        private static readonly int Repeat = 4; //repeat times every evaluation
        private static readonly int Interval = 10 * 60 * 1000;  //evaluate proxies every 15 minutes
        private Timer timer = null;
        private State state = null;
        private List<Server> servers;

        public static string AvailabilityStatisticsFile;

        //static constructor to initialize every public static fields before refereced
        static AvailabilityStatistics()
        {
            string temppath = Utils.GetTempPath();
            AvailabilityStatisticsFile = Path.Combine(temppath, StatisticsFilesName);
        }

        public bool Set(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    if (timer?.Change(0, Interval) == null)
                    {
                        state = new State();
                        timer = new Timer(Evaluate, state, 0, Interval);
                    }
                }
                else
                {
                    timer?.Dispose();
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
            Ping ping = new Ping();
            State state = (State) obj;
            foreach (var server in servers)
            {
                Logging.Debug("eveluating " + server.FriendlyName());
                foreach (var _ in Enumerable.Range(0, Repeat))
                {
                    //TODO: do simple analyze of data to provide friendly message, like package loss.
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    //ICMP echo. we can also set options and special bytes
                    //seems no need to use SendPingAsync
                    try
                    {
                        PingReply reply = ping.Send(server.server, Timeout);
                        state.data = new List<KeyValuePair<string, string>>();
                        state.data.Add(new KeyValuePair<string, string>("Timestamp", timestamp));
                        state.data.Add(new KeyValuePair<string, string>("Server", server.FriendlyName()));
                        state.data.Add(new KeyValuePair<string, string>("Status", reply.Status.ToString()));
                        state.data.Add(new KeyValuePair<string, string>("RoundtripTime", reply.RoundtripTime.ToString()));
                        //state.data.Add(new KeyValuePair<string, string>("data", reply.Buffer.ToString())); // The data of reply
                        Append(state.data);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"An exception occured when eveluating {server.FriendlyName()}");
                        Logging.LogUsefulException(e);
                    }
                }
            }
        }

        private static void Append(List<KeyValuePair<string, string>> data)
        {
            string dataLine = string.Join(Delimiter, data.Select(kv => kv.Value).ToArray());
            string[] lines;
            if (!File.Exists(AvailabilityStatisticsFile))
            {
                string headerLine = string.Join(Delimiter, data.Select(kv => kv.Key).ToArray());
                lines = new string[] { headerLine, dataLine };
            }
            else
            {
                lines = new string[] { dataLine };
            }
            File.AppendAllLines(AvailabilityStatisticsFile, lines);
        }

        internal void UpdateConfiguration(Configuration _config)
        {
            Set(_config.availabilityStatistics);
            servers = _config.configs;
        }

        private class State
        {
            public List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
        }
    }
}
