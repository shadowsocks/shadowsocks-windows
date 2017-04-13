using System;

namespace Shadowsocks.Model
{
    [Serializable]
    public class PrivoxyConfig
    {
        public bool enableCustomPort;
        public int listenPort;

        public PrivoxyConfig()
        {
            enableCustomPort = false;
            listenPort = 0;
        }
    }
}
