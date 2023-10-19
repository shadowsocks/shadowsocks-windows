using Shadowsocks.Models;
using Shadowsocks.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Shadowsocks.CLI.Client;

public class Legacy
{
    private TCPListener? _tcpListener;
    private UdpListener? _udpListener;

    public void Start(string listenSocks, string serverAddress, int serverPort, string method, string password, string? plugin, string? pluginOpts, string? pluginArgs)
    {
        var localEP = IPEndPoint.Parse(listenSocks);
        var server = new Server()
        {
            Host = serverAddress,
            Port = serverPort,
            Method = method,
            Password = password,
            Plugin = plugin,
            PluginOpts = pluginOpts,
        };
        if (!string.IsNullOrEmpty(plugin) && !string.IsNullOrEmpty(pluginArgs))
        {
            var processStartInfo = new ProcessStartInfo(plugin, pluginArgs);
            server.PluginArgs = processStartInfo.ArgumentList.ToList();
        }

        var tcpRelay = new TcpRelay(server);
        _tcpListener = new TCPListener(localEP, new List<IStreamService>()
        {
            tcpRelay,
        });
        _tcpListener.Start();

        var udpRelay = new UdpRelay(server);
        _udpListener = new UdpListener(localEP, new List<IDatagramService>()
        {
            udpRelay,
        });
        _udpListener.Start();
    }

    public void Stop()
    {
        _tcpListener?.Stop();
        _udpListener?.Stop();
    }
}