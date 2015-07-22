using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Controller.Strategy
{
    class HighAvailabilityStrategy : IStrategy
    {
        protected Server _currentServer;
        protected Dictionary<Server, ServerStatus> _serverStatus;
        ShadowsocksController _controller;
        Random _random;

        public class ServerStatus
        {
            // time interval between SYN and SYN+ACK
            public TimeSpan latency;

            // last time anything received
            public DateTime lastRead;

            // last time anything sent
            public DateTime lastWrite;

            // connection refused or closed before anything received
            public DateTime lastFailure;

            public Server server;
        }

        /**
         * if last failure is > 10 min
         * and (last write > last read) and (now - last read <  5s)  // means not stuck
         * choose the lowest latency
         */

        public HighAvailabilityStrategy(ShadowsocksController controller)
        {
            _controller = controller;
            _random = new Random();
            _serverStatus = new Dictionary<Server, ServerStatus>();
        }

        public string Name
        {
            get { return I18N.GetString("High Availability"); }
        }

        public string ID
        {
            get { return "com.shadowsocks.strategy.ha"; }
        }

        public void ReloadServers()
        {
            // make a copy to avoid locking
            var newServerStatus = new Dictionary<Server, ServerStatus>(_serverStatus);

            foreach (var server in _controller.GetCurrentConfiguration().configs)
            {
                if (!newServerStatus.ContainsKey(server))
                {
                    var status = new ServerStatus();
                    status.server = server;
                    newServerStatus[server] = status;
                }
            }
            _serverStatus = newServerStatus;

            // just leave removed servers there
            
            // TODO
            _currentServer = _controller.GetCurrentConfiguration().configs[0];
        }

        public Server GetAServer(IStrategyCallerType type, System.Net.IPEndPoint localIPEndPoint)
        {
            return _currentServer;
        }

        public void UpdateLatency(Model.Server server, TimeSpan latency)
        {
            Logging.Debug(String.Format("latency: {0} {1}", server.FriendlyName(), latency));

            ServerStatus status;
            if (_serverStatus.TryGetValue(server, out status))
            {
                status.latency = latency;
            }
        }

        public void UpdateLastRead(Model.Server server)
        {
            Logging.Debug(String.Format("last read: {0}", server.FriendlyName()));

            ServerStatus status;
            if (_serverStatus.TryGetValue(server, out status))
            {
                status.lastRead = DateTime.Now;
            }
        }

        public void UpdateLastWrite(Model.Server server)
        {
            Logging.Debug(String.Format("last write: {0}", server.FriendlyName()));

            ServerStatus status;
            if (_serverStatus.TryGetValue(server, out status))
            {
                status.lastWrite = DateTime.Now;
            }
        }

        public void SetFailure(Model.Server server)
        {
            Logging.Debug(String.Format("failure: {0}", server.FriendlyName()));

            ServerStatus status;
            if (_serverStatus.TryGetValue(server, out status))
            {
                status.lastFailure = DateTime.Now;
            }
        }
    }
}
