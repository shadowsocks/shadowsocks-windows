using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using SimpleJson;
using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Server
    {
        public string server;
        public int server_port;
        public int local_port;
        public string password;
        public string method;
        public string remarks;

        public string FriendlyName()
        {
            if (string.IsNullOrEmpty(server))
            {
                return I18N.GetString("New server");
            }
            if (string.IsNullOrEmpty(remarks))
            {
                return server + ":" + server_port;
            }
            else
            {
                return remarks + " (" + server + ":" + server_port + ")";
            }
        }
    }
}
