using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Web;

namespace Shadowsocks.Controller
{
    class APIServer : Listener.Service
    {
        private ShadowsocksController _controller;
        private Configuration _config;

        public const int RecvSize = 16384;
        private byte[] connetionRecvBuffer = new byte[RecvSize];
        string connection_request;
        Socket _local;

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
                        if (line.IndexOf("auth=" + _config.localAuthPassword) > 0)
                        {
                            if (line.IndexOf(" /api?") > 0)
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
                }
                if (hostMatch && pathMatch)
                {
                    _local = socket;
                    if (CheckEnd(request))
                    {
                        process(request);
                    }
                    else
                    {
                        connection_request = request;
                        socket.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                            new AsyncCallback(HttpHandshakeRecv), null);
                    }
                    return true;
                }
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private bool CheckEnd(string request)
        {
            int newline_pos = request.IndexOf("\r\n\r\n");
            if (request.StartsWith("POST "))
            {
                if (newline_pos > 0)
                {
                    string head = request.Substring(0, newline_pos);
                    string tail = request.Substring(newline_pos + 4);
                    string[] lines = head.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Content-Length: "))
                        {
                            try
                            {
                                int length = int.Parse(line.Substring("Content-Length: ".Length));
                                if (length <= tail.Length)
                                    return true;
                            }
                            catch (FormatException)
                            {
                                break;
                            }
                        }
                    }
                    return false;
                }
            }
            else
            {
                if (newline_pos + 4 == request.Length)
                {
                    return true;
                }
            }
            return false;
        }

        private void HttpHandshakeRecv(IAsyncResult ar)
        {
            try
            {
                int bytesRead = _local.EndReceive(ar);
                if (bytesRead > 0)
                {
                    string request = Encoding.UTF8.GetString(connetionRecvBuffer, 0, bytesRead);
                    connection_request += request;
                    if (CheckEnd(connection_request))
                    {
                        process(connection_request);
                    }
                    else
                    {
                        _local.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                            new AsyncCallback(HttpHandshakeRecv), null);
                    }
                }
                else
                {
                    Console.WriteLine("APIServer: failed to recv data in HttpHandshakeRecv");
                    _local.Shutdown(SocketShutdown.Both);
                    _local.Close();
                }
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                try
                {
                    _local.Shutdown(SocketShutdown.Both);
                    _local.Close();
                }
                catch
                { }
            }
        }

        protected string process(string request)
        {
            string req;
            req = request.Substring(0, request.IndexOf("\r\n"));
            req = req.Substring(req.IndexOf("api?") + 4);
            req = req.Substring(0, req.IndexOf(" "));

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
            if (request.IndexOf("POST ") == 0)
            {
                string post_params = request.Substring(request.IndexOf("\r\n\r\n") + 4);
                get_params = post_params.Split('&');
                foreach (string p in get_params)
                {
                    if (p.IndexOf('=') > 0)
                    {
                        int index = p.IndexOf('=');
                        string key, val;
                        key = p.Substring(0, index);
                        val = p.Substring(index + 1);
                        params_dict[key] = Util.Utils.urlDecode(val);
                    }
                }
            }
            if (params_dict.ContainsKey("token") && params_dict.ContainsKey("app")
                && _config.token.ContainsKey(params_dict["app"]) && _config.token[params_dict["app"]] == params_dict["token"])
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
                        _local.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), _local);
                        return "";
                    }
                    else if (params_dict["action"] == "config")
                    {
                        if (params_dict.ContainsKey("config"))
                        {
                            string content = "";
                            string ret_code = "200 OK";
                            if (!_controller.SaveServersConfig(params_dict["config"]))
                            {
                                ret_code = "403 Forbid";
                            }
                            string text = String.Format(@"HTTP/1.1 {0}
Server: ShadowsocksR
Content-Type: text/plain
Content-Length: {1}
Connection: Close

", ret_code, System.Text.Encoding.UTF8.GetBytes(content).Length) + content;
                            byte[] response = System.Text.Encoding.UTF8.GetBytes(text);
                            _local.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), _local);
                            return "";
                        }
                        else
                        {
                            Dictionary<string, string> token = _config.token;
                            _config.token = new Dictionary<string, string>();
                            string content = SimpleJson.SimpleJson.SerializeObject(_config);
                            _config.token = token;

                            string text = String.Format(@"HTTP/1.1 200 OK
Server: ShadowsocksR
Content-Type: text/plain
Content-Length: {0}
Connection: Close

", System.Text.Encoding.UTF8.GetBytes(content).Length) + content;
                            byte[] response = System.Text.Encoding.UTF8.GetBytes(text);
                            _local.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), _local);
                            return "";
                        }
                    }
                }
            }
            {
                byte[] response = System.Text.Encoding.UTF8.GetBytes("");
                _local.BeginSend(response, 0, response.Length, 0, new AsyncCallback(SendCallback), _local);
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
