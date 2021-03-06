using Shadowsocks.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.CLI.Client
{
    public class Pipelines
    {
        private TcpPipeListener? _tcpPipeListener;
        
        public Task Start(string listenSocks, string serverAddress, int serverPort, string method, string? password, string? key, string? plugin, string? pluginOpts, string? pluginArgs)
        {
            // TODO
            var localEP = IPEndPoint.Parse(listenSocks);
            var remoteEp = new DnsEndPoint(serverAddress, serverPort);
            byte[]? mainKey = null;
            if (!string.IsNullOrEmpty(key))
                mainKey = Encoding.UTF8.GetBytes(key);
            _tcpPipeListener = new(localEP);
            return _tcpPipeListener.Start(localEP, remoteEp, method, password, mainKey);
        }

        public void Stop() => _tcpPipeListener?.Stop();
    }
}
