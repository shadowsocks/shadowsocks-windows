using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Shadowsocks.Model;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Newtonsoft.Json;
using Shadowsocks.Model;
using Timer = System.Threading.Timer;

namespace Shadowsocks.Controller.Strategy
{
    using DataUnit = KeyValuePair<string, string>;
    using DataList = List<KeyValuePair<string, string>>;

    class StatisticsStrategy : IStrategy
    {
        private readonly ShadowsocksController _controller;
        private Server _currentServer;
        private readonly Timer _timer;
        private Dictionary<string, List<StatisticsRawData>> _rawStatistics;
        private int ChoiceKeptMilliseconds
            => (int) TimeSpan.FromMinutes(_controller.StatisticsConfiguration.ChoiceKeptMinutes).TotalMilliseconds;
        private const int RetryInterval = 2*60*1000; //retry 2 minutes after failed

        public StatisticsStrategy(ShadowsocksController controller)
        {
            _controller = controller;
            var servers = controller.GetCurrentConfiguration().configs;
            var randomIndex = new Random().Next() % servers.Count();
            _currentServer = servers[randomIndex];  //choose a server randomly at first
            _timer = new Timer(ReloadStatisticsAndChooseAServer);
        }

        private void ReloadStatisticsAndChooseAServer(object obj)
        {
            Logging.Debug("Reloading statistics and choose a new server....");
            var servers = _controller.GetCurrentConfiguration().configs;
            LoadStatistics();
            ChooseNewServer(servers);
        }

        private void LoadStatistics()
        {
            try
            {
                var path = AvailabilityStatistics.AvailabilityStatisticsFile;
                Logging.Debug($"loading statistics from {path}");
                if (!File.Exists(path))
                {
                    LogWhenEnabled($"statistics file does not exist, try to reload {RetryInterval/60/1000} minutes later");
                    _timer.Change(RetryInterval, ChoiceKeptMilliseconds);
                    return;
                }
                _rawStatistics = (from l in File.ReadAllLines(path)
                    .Skip(1)
                    let strings = l.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
                    let rawData = new StatisticsRawData
                    {
                        Timestamp = strings[0],
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

        //return the score by data
        //server with highest score will be choosen
        private float GetScore(IEnumerable<StatisticsRawData> rawDataList)
        {
            var config = _controller.StatisticsConfiguration;
            if (config.ByIsp)
            {
                var current = AvailabilityStatistics.GetGeolocationAndIsp().Result;
                rawDataList = rawDataList.Where(data => data.Geolocation == current[0].Value || data.Geolocation == AvailabilityStatistics.State.Unknown);
                rawDataList = rawDataList.Where(data => data.ISP == current[1].Value || data.ISP == AvailabilityStatistics.State.Unknown);
                if (rawDataList.LongCount() == 0) return 0; 
            }
            if (config.ByHourOfDay)
            {
                var currentHour = DateTime.Now.Hour;
                rawDataList = rawDataList.Where(data =>
                {
                    DateTime dateTime;
                    DateTime.TryParseExact(data.Timestamp, AvailabilityStatistics.DateTimePattern, null,
                        DateTimeStyles.None, out dateTime);
                    var result = dateTime.Hour.Equals(currentHour);
                    return result;
                });
                if (rawDataList.LongCount() == 0) return 0; 
            }
            var dataList = rawDataList as IList<StatisticsRawData> ?? rawDataList.ToList();
            var serverName = dataList[0]?.ServerName;
            var SuccessTimes = (float) dataList.Count(data => data.ICMPStatus.Equals(IPStatus.Success.ToString()));
            var TimedOutTimes = (float) dataList.Count(data => data.ICMPStatus.Equals(IPStatus.TimedOut.ToString()));
            var statisticsData = new StatisticsData()
            {
                PackageLoss = TimedOutTimes / (SuccessTimes + TimedOutTimes) * 100,
                AverageResponse = Convert.ToInt32(dataList.Average(data => data.RoundtripTime)),
                MinResponse = dataList.Min(data => data.RoundtripTime),
                MaxResponse = dataList.Max(data => data.RoundtripTime)
            };
            float factor;
            float score = 0;
            if (!config.Calculations.TryGetValue("PackageLoss", out factor)) factor = 0;
            score += statisticsData.PackageLoss*factor;
            if (!config.Calculations.TryGetValue("AverageResponse", out factor)) factor = 0;
            score += statisticsData.AverageResponse*factor;
            if (!config.Calculations.TryGetValue("MinResponse", out factor)) factor = 0;
            score += statisticsData.MinResponse*factor;
            if (!config.Calculations.TryGetValue("MaxResponse", out factor)) factor = 0;
            score += statisticsData.MaxResponse*factor;
            Logging.Debug($"{serverName}  {JsonConvert.SerializeObject(statisticsData)}");
            return score;
        }

        class StatisticsRawData
        {
            public string Timestamp;
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

        private void ChooseNewServer(List<Server> servers)
        {
            if (_rawStatistics == null || servers.Count == 0)
            {
                return;
            }
            try
            {
                var bestResult = (from server in servers
                                  let name = server.FriendlyName()
                                  where _rawStatistics.ContainsKey(name)
                                  select new
                                  {
                                      server,
                                      score = GetScore(_rawStatistics[name])
                                  }
                                  ).Aggregate((result1, result2) => result1.score > result2.score ? result1 : result2);

                if (!_currentServer.Equals(bestResult.server)) //output when enabled
                {
                   LogWhenEnabled($"Switch to server: {bestResult.server.FriendlyName()} by statistics: score {bestResult.score}");
                }
                _currentServer = bestResult.server;
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        private void LogWhenEnabled(string log)
        {
            if (_controller.GetCurrentStrategy()?.ID == ID) //output when enabled
            {
                Console.WriteLine(log);
            }
        }

        public string ID => "com.shadowsocks.strategy.scbs";

        public string Name => I18N.GetString("Choose By Total Package Loss");

        public Server GetAServer(IStrategyCallerType type, IPEndPoint localIPEndPoint)
        {
            var oldServer = _currentServer;
            if (oldServer == null)
            {
                ChooseNewServer(_controller.GetCurrentConfiguration().configs);
            }
            if (oldServer != _currentServer)
            {
            }
            return _currentServer;  //current server cached for CachedInterval
        }

        public void ReloadServers()
        {
            ChooseNewServer(_controller.GetCurrentConfiguration().configs);
            _timer?.Change(0, ChoiceKeptMilliseconds);
        }

        public void SetFailure(Server server)
        {
            Logging.Debug($"failure: {server.FriendlyName()}");
        }

        public void UpdateLastRead(Server server)
        {
            //TODO: combine this part of data with ICMP statics
        }

        public void UpdateLastWrite(Server server)
        {
            //TODO: combine this part of data with ICMP statics
        }

        public void UpdateLatency(Server server, TimeSpan latency)
        {
            //TODO: combine this part of data with ICMP statics
        }

    }
}
