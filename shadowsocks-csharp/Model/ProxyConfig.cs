using Shadowsocks.View;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Shadowsocks.Model
{
    [Serializable]
    public class ProxyConfig
    {
        public bool useProxy;
        public string proxyServer;
        public int proxyPort;

        public ProxyConfig()
        {
            useProxy = false;
            proxyServer = "";
            proxyPort = 0;
        }
    }
}
