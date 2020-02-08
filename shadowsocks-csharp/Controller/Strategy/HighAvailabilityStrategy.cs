using NLog;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;

namespace Shadowsocks.Controller.Strategy
{
    class HighAvailabilityStrategy : IStrategy
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        protected ServerStatus _currentServer;
        protected Dictionary<Server, ServerStatus> _serverStatus;
        ShadowsocksController _controller;
        Random _random;

        public class ServerStatus
        {
            // time interval between SYN and SYN+ACK
            public TimeSpan latency;
            public DateTime lastTimeDetectLatency;

            // last time anything received
            public DateTime lastRead;

            // last time anything sent
            public DateTime lastWrite;

            // connection refused or closed before anything received
            public DateTime lastFailure;

            public Server server;

            public double score;
        }

        public HighAvailabilityStrategy(ShadowsocksController controller)
        {
            _controller = controller;
            _random = new Random();
            _serverStatus = new Dictionary<Server, ServerStatus>();
        }

        public string Name => I18N.GetString("High Availability");

        public string ID => "com.shadowsocks.strategy.ha";

        public void ReloadServers()
        {
            // make a copy to avoid locking
            Dictionary<Server, ServerStatus> newServerStatus = new Dictionary<Server, ServerStatus>(_serverStatus);

            foreach (Server server in _controller.GetCurrentConfiguration().configs)
            {
                if (!newServerStatus.ContainsKey(server))
                {
                    ServerStatus status = new ServerStatus
                    {
                        server = server,
                        lastFailure = DateTime.MinValue,
                        lastRead = DateTime.Now,
                        lastWrite = DateTime.Now,
                        latency = new TimeSpan(0, 0, 0, 0, 10),
                        lastTimeDetectLatency = DateTime.Now
                    };
                    newServerStatus[server] = status;
                }
                else
                {
                    // update settings for existing server
                    newServerStatus[server].server = server;
                }
            }
            _serverStatus = newServerStatus;

            ChooseNewServer();
        }

        public Server GetAServer(IStrategyCallerType type, System.Net.IPEndPoint localIPEndPoint, EndPoint destEndPoint)
        {
            if (type == IStrategyCallerType.TCP)
            {
                ChooseNewServer();
            }
            if (_currentServer == null)
            {
                return null;
            }
            return _currentServer.server;
        }

        /**
         * once failed, try after 5 min
         * and (last write - last read) < 5s
         * and (now - last read) <  5s  // means not stuck
         * and latency < 200ms, try after 30s
         */
        public void ChooseNewServer()
        {
            ServerStatus oldServer = _currentServer;
            List<ServerStatus> servers = new List<ServerStatus>(_serverStatus.Values);
            DateTime now = DateTime.Now;
            foreach (ServerStatus status in servers)
            {
                // all of failure, latency, (lastread - lastwrite) normalized to 1000, then
                // 100 * failure - 2 * latency - 0.5 * (lastread - lastwrite)
                status.score =
                    100 * 1000 * Math.Min(5 * 60, (now - status.lastFailure).TotalSeconds)
                    - 2 * 5 * (Math.Min(2000, status.latency.TotalMilliseconds) / (1 + (now - status.lastTimeDetectLatency).TotalSeconds / 30 / 10) +
                    -0.5 * 200 * Math.Min(5, (status.lastRead - status.lastWrite).TotalSeconds));
                logger.Debug(string.Format("server: {0} latency:{1} score: {2}", status.server.ToString(), status.latency, status.score));
            }
            ServerStatus max = null;
            foreach (ServerStatus status in servers)
            {
                if (max == null)
                {
                    max = status;
                }
                else
                {
                    if (status.score >= max.score)
                    {
                        max = status;
                    }
                }
            }
            if (max != null)
            {
                if (_currentServer == null || max.score - _currentServer.score > 200)
                {
                    _currentServer = max;
                    logger.Info($"HA switching to server: {_currentServer.server.ToString()}");
                }
            }
        }

        public void UpdateLatency(Model.Server server, TimeSpan latency)
        {
            logger.Debug($"latency: {server.ToString()} {latency}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.latency = latency;
                status.lastTimeDetectLatency = DateTime.Now;
            }
        }

        public void UpdateLastRead(Model.Server server)
        {
            logger.Debug($"last read: {server.ToString()}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.lastRead = DateTime.Now;
            }
        }

        public void UpdateLastWrite(Model.Server server)
        {
            logger.Debug($"last write: {server.ToString()}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.lastWrite = DateTime.Now;
            }
        }

        public void SetFailure(Model.Server server)
        {
            logger.Debug($"failure: {server.ToString()}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.lastFailure = DateTime.Now;
            }
        }
    }
}
