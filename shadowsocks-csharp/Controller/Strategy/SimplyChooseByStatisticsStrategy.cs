using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Shadowsocks.Model;

namespace Shadowsocks.Controller.Strategy
{
    class SimplyChooseByStatisticsStrategy : IStrategy
    {
        private readonly ShadowsocksController _controller;
        private Server _currentServer;
        private readonly Timer _timer;
        private Dictionary<string, StatisticsData> _statistics;
        private const int CachedInterval = 30*60*1000; //choose a new server every 30 minutes
        private const int RetryInterval = 2*60*1000; //choose a new server every 30 minutes

        public SimplyChooseByStatisticsStrategy(ShadowsocksController controller)
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

        /*
        return a dict:
        {
            'ServerFriendlyName1':StatisticsData,
            'ServerFriendlyName2':...
        }
        */
        private void LoadStatistics()
        {
            try
            {
                var path = AvailabilityStatistics.AvailabilityStatisticsFile;
                Logging.Debug($"loading statistics from {path}");
                if (!File.Exists(path))
                {
                    LogWhenEnabled($"statistics file does not exist, try to reload {RetryInterval} minutes later");
                    _timer.Change(RetryInterval, CachedInterval);
                    return;
                }
                _statistics = (from l in File.ReadAllLines(path)
                                  .Skip(1)
                                  let strings = l.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                                  let rawData = new
                                  {
                                      ServerName = strings[1],
                                      IPStatus = strings[2],
                                      RoundtripTime = int.Parse(strings[3])
                                  }
                                  group rawData by rawData.ServerName into server
                                  select new
                                  {
                                      ServerName = server.Key,
                                      data = new StatisticsData
                                      {
                                          SuccessTimes = server.Count(data => IPStatus.Success.ToString().Equals(data.IPStatus)),
                                          TimedOutTimes = server.Count(data => IPStatus.TimedOut.ToString().Equals(data.IPStatus)),
                                          AverageResponse = Convert.ToInt32(server.Average(data => data.RoundtripTime)),
                                          MinResponse = server.Min(data => data.RoundtripTime),
                                          MaxResponse = server.Max(data => data.RoundtripTime)
                                      }
                                  }).ToDictionary(server => server.ServerName, server => server.data);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        //return the score by data
        //server with highest score will be choosen
        private static double GetScore(StatisticsData data)
        {
            return (double)data.SuccessTimes / (data.SuccessTimes + data.TimedOutTimes); //simply choose min package loss
        }

        private class StatisticsData
        {
            public int SuccessTimes;
            public int TimedOutTimes;
            public int AverageResponse;
            public int MinResponse;
            public int MaxResponse;
        }

        private void ChooseNewServer(List<Server> servers)
        {
            if (_statistics == null)
            {
                return;
            }
            try
            {
                var bestResult = (from server in servers
                                  let name = server.FriendlyName()
                                  where _statistics.ContainsKey(name)
                                  select new
                                  {
                                      server,
                                      score = GetScore(_statistics[name])
                                  }
                                  ).Aggregate((result1, result2) => result1.score > result2.score ? result1 : result2);

                if (!_currentServer.Equals(bestResult.server)) //output when enabled
                {  
                   LogWhenEnabled($"Switch to server: {bestResult.server.FriendlyName()} by package loss:{1 - bestResult.score}");
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
            return _currentServer;  //current server cached for CachedInterval
        }

        public void ReloadServers()
        {
            ChooseNewServer(_controller.GetCurrentConfiguration().configs);
            _timer?.Change(0, CachedInterval);
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
