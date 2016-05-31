using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    class APIServer : Listener.Service
    {
        private ShadowsocksController _controller;
        private Configuration _config;

        public APIServer(ShadowsocksController controller, Configuration config)
        {
            _controller = controller;
            _config = config;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket)
        {
            try
            {
                string request = Encoding.UTF8.GetString(firstPacket, 0, length);
                string[] lines = request.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                bool hostMatch = false, pathMatch = false;
                string req = "";
                foreach (string line in lines)
                {
                    string[] kv = line.Split(new char[] { ':' }, 2);
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "Host")
                        {
                            if (kv[1].Trim() == ((IPEndPoint)socket.LocalEndPoint).ToString())
                            {
                                hostMatch = true;
                            }
                        }
                    }
                    else if (kv.Length == 1)
                    {
                        if (line.IndexOf("api?") > 0)
                        {
                            req = line.Substring(line.IndexOf("api?") + 4);
                            if (line.IndexOf("GET ") == 0 || line.IndexOf("POST ") == 0)
                            {
                                pathMatch = true;
                                req = req.Substring(0, req.IndexOf(" "));
                            }
                        }
                    }
                }
                if (hostMatch && pathMatch)
                {
                    process(firstPacket, length, req, socket);
                    return true;
                }
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        protected string process(byte[] firstPacket, int length, string req, Socket socket)
        {
            string[] get_params = req.Split('&');
            Dictionary<string, string> params_dict = new Dictionary<string, string>();
            foreach (string p in get_params)
            {
                if (p.IndexOf('=') > 0)
                {
                    int index = p.IndexOf('=');
                    string key, val;
                    key = p.Substring(0, index);
                    val = p.Substring(index + 1);
                    params_dict[key] = val;
                }
            }
            if (params_dict.ContainsKey("token") && params_dict["token"] == "")
            {
                if (params_dict.ContainsKey("action"))
                {
                    if (params_dict["action"] == "statistics")
                    {
                        Configuration config = _config;
                        ServerSpeedLogShow[] _ServerSpeedLogList = new ServerSpeedLogShow[config.configs.Count];
                        Dictionary<string, object> servers = new Dictionary<string, object>();
                        for (int i = 0; i < config.configs.Count; ++i)
                        {
                            _ServerSpeedLogList[i] = config.configs[i].ServerSpeedLog().Translate();
                            servers[config.configs[i].id] = _ServerSpeedLogList[i];
                        }
                        string content = SimpleJson.SimpleJson.SerializeObject(servers);

                        string text = String.Format(@"HTTP/1.1 200 OK
Server: ShadowsocksR
Content-Type: text/plain
Content-Length: {0}
Connection: Close

", System.Text.Encoding.UTF8.GetBytes(content).Length) + content;
                        byte[] response = System.Text.Encoding.UTF8.GetBytes(text);
                        socket.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), socket);
                        return "";
                    }
                    else if (params_dict["action"] == "config")
                    {
                        string content = SimpleJson.SimpleJson.SerializeObject(_config);

                        string text = String.Format(@"HTTP/1.1 200 OK
Server: ShadowsocksR
Content-Type: text/plain
Content-Length: {0}
Connection: Close

", System.Text.Encoding.UTF8.GetBytes(content).Length) + content;
                        byte[] response = System.Text.Encoding.UTF8.GetBytes(text);
                        socket.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), socket);
                        return "";
                    }
                }
            }
            {
                byte[] response = System.Text.Encoding.UTF8.GetBytes("");
                socket.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), socket);
            }
            return "";
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket conn = (Socket)ar.AsyncState;
            try
            {
                conn.Shutdown(SocketShutdown.Both);
                conn.Close();
            }
            catch
            { }
        }
    }
}
