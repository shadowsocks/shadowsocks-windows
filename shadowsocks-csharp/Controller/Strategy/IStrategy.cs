using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Shadowsocks.Controller.Strategy
{
    public enum IStrategyCallerType
    {
        TCP,
        UDP
    }

    public interface IStrategy
    {
        string Name { get; }
        string ID { get; }

        Server GetAServer(IStrategyCallerType type, IPEndPoint localIPEndPoint);

        void UpdateLatency(Server server);

        void UpdateLastRead(Server server);

        void UpdateLastWrite(Server server);

        void SetFailure(Server server);
    }
}
