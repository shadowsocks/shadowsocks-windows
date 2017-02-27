using System;

namespace Shadowsocks.Model
{
    [Serializable]
    public class ProxyConfig
    {
        public const int PROXY_SOCKS5 = 0;
        public const int PROXY_HTTP = 1;

        public const int MaxProxyTimeoutSec = 10;
        private const int DefaultProxyTimeoutSec = 3;

        public bool useProxy;
        public int proxyType;
        public string proxyServer;
        public int proxyPort;
        public int proxyTimeout;

        public ProxyConfig()
        {
            useProxy = false;
            proxyType = PROXY_SOCKS5;
            proxyServer = "";
            proxyPort = 0;
            proxyTimeout = DefaultProxyTimeoutSec;
        }

        public void CheckConfig()
        {
            if (proxyType < PROXY_SOCKS5 || proxyType > PROXY_HTTP)
            {
                proxyType = PROXY_SOCKS5;
            }
        }
    }
}
