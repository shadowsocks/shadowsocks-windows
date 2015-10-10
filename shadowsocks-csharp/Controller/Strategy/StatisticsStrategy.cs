using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Newtonsoft.Json;
using Shadowsocks.Model;

namespace Shadowsocks.Controller.Strategy
{
    class StatisticsStrategy : IStrategy
    {
        private readonly ShadowsocksController _controller;
        private Server _currentServer;
        private readonly Timer _timer;
        private Dictionary<string, List<AvailabilityStatistics.RawStatisticsData>> _filteredStatistics;
        private int ChoiceKeptMilliseconds
            => (int) TimeSpan.FromMinutes(_controller.StatisticsConfiguration.ChoiceKeptMinutes).TotalMilliseconds;

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
            _filteredStatistics = _controller.availabilityStatistics.RawStatistics ?? _filteredStatistics ?? new Dictionary<string, List<AvailabilityStatistics.RawStatisticsData>>();
        }

        //return the score by data
        //server with highest score will be choosen
        private float GetScore(string serverName)
        {
            var config = _controller.StatisticsConfiguration;
            List<AvailabilityStatistics.RawStatisticsData> dataList;
            if (_filteredStatistics == null || !_filteredStatistics.TryGetValue(serverName, out dataList)) return 0;
            var successTimes = (float) dataList.Count(data => data.ICMPStatus.Equals(IPStatus.Success.ToString()));
            var timedOutTimes = (float) dataList.Count(data => data.ICMPStatus.Equals(IPStatus.TimedOut.ToString()));
            var statisticsData = new AvailabilityStatistics.StatisticsData
            {
                PackageLoss = timedOutTimes/(successTimes + timedOutTimes)*100,
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

        private void ChooseNewServer(List<Server> servers)
        {
            if (_filteredStatistics == null || servers.Count == 0)
            {
                return;
            }
            try
            {
                var bestResult = (from server in servers
                                  let name = server.FriendlyName()
                                  where _filteredStatistics.ContainsKey(name)
                                  select new
                                  {
                                      server,
                                      score = GetScore(name)
                                  }
                                  ).Aggregate((result1, result2) => result1.score > result2.score ? result1 : result2);

               LogWhenEnabled($"Switch to server: {bestResult.server.FriendlyName()} by statistics: score {bestResult.score}");
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
