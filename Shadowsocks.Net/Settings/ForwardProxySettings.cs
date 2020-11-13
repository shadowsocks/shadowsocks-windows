using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Net.Settings
{
    public class ForwardProxySettings
    {
        public bool NoProxy { get; set; }
        public bool UseSocks5Proxy { get; set; }
        public bool UseHttpProxy { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ForwardProxySettings()
        {
            NoProxy = true;
            UseSocks5Proxy = false;
            UseHttpProxy = false;
            Address = "";
            Port = 1088;
            Username = "";
            Password = "";
        }
    }
}
