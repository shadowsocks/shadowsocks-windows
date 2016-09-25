using System;

namespace Shadowsocks.Model
{
    [Serializable]
    public class ProxyConfig
    {
        public const int PROXY_SOCKS5 = 0;
        public const int PROXY_HTTP = 1;

        public bool useProxy;
        public int proxyType;
        public string proxyServer;
        public int proxyPort;

        public ProxyConfig()
        {
            useProxy = false;
            proxyType = PROXY_SOCKS5;
            proxyServer = "";
            proxyPort = 0;
        }
    }
}
