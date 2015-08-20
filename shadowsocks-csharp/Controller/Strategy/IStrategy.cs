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

    /*
     * IStrategy
     *
     * Subclasses must be thread-safe
     */
    public interface IStrategy
    {
        string Name { get; }

        string ID { get; }

        /*
         * Called when servers need to be reloaded, i.e. new configuration saved
         */
        void ReloadServers();

        /*
         * Get a new server to use in TCPRelay or UDPRelay
         */
        Server GetAServer(IStrategyCallerType type, IPEndPoint localIPEndPoint);

        /*
         * TCPRelay will call this when latency of a server detected
         */
        void UpdateLatency(Server server, TimeSpan latency);

        /*
         * TCPRelay will call this when reading from a server
         */
        void UpdateLastRead(Server server);

        /*
         * TCPRelay will call this when writing to a server
         */
        void UpdateLastWrite(Server server);

        /*
         * TCPRelay will call this when fatal failure detected
         */
        void SetFailure(Server server);
    }
}
