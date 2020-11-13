using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Net.Settings
{
    public class NetSettings
    {
        public bool EnableSocks5 { get; set; }
        public bool EnableHttp { get; set; }
        public string Socks5ListeningAddress { get; set; }
        public string HttpListeningAddress { get; set; }
        public int Socks5ListeningPort { get; set; }
        public int HttpListeningPort { get; set; }

        public ForwardProxySettings ForwardProxy { get; set; }
        
        public NetSettings()
        {
            EnableSocks5 = true;
            EnableHttp = true;
            Socks5ListeningAddress = "::1";
            HttpListeningAddress = "::1";
            Socks5ListeningPort = 1080;
            HttpListeningPort = 1080;

            ForwardProxy = new ForwardProxySettings();
        }
    }
}
