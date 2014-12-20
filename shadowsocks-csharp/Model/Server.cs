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

        public string FriendlyName
        {
            get
            {
                if (string.IsNullOrEmpty(server))
                {
                    return I18N.GetString("New server");
                }
                return string.IsNullOrEmpty(remarks) ? server + ":" + server_port : server + ":" + server_port + " (" + remarks + ")";
            }
        }
    }
}
