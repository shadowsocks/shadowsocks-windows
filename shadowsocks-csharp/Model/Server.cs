using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
#if !_CONSOLE
using SimpleJson;
#endif
using Shadowsocks.Controller;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Encryption;

namespace Shadowsocks.Model
{
    public class DnsBuffer
    {
        public IPAddress ip;
        public DateTime updateTime;
        public string host;
        public bool isExpired(string host)
        {
            if (updateTime == null) return true;
            if (this.host != host) return true;
            return (DateTime.Now - updateTime).TotalMinutes > 10;
        }
        public void UpdateDns(string host, IPAddress ip)
        {
            updateTime = DateTime.Now;
            this.ip = new IPAddress(ip.GetAddressBytes());
            this.host = (string)host.Clone();
        }
    }
    public class Connections
    {
        private System.Collections.Generic.Dictionary<Socket, Int32> sockets = new Dictionary<Socket, int>();
        public bool AddRef(Socket socket)
        {
            lock (this)
            {
                if (sockets.ContainsKey(socket))
                {
                    sockets[socket] += 1;
                }
                else
                {
                    sockets[socket] = 1;
                }
                return true;
            }
        }
        public bool DecRef(Socket socket)
        {
            lock (this)
            {
                if (sockets.ContainsKey(socket))
                {
                    sockets[socket] -= 1;
                    if (sockets[socket] == 0)
                    {
                        sockets.Remove(socket);
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
        }
        public void CloseAll()
        {
            Socket[] s;
            lock (this)
            {
                s = new Socket[sockets.Count];
                sockets.Keys.CopyTo(s, 0);
            }
            foreach (Socket socket in s)
            {
                try
                {
                    //socket.Shutdown(SocketShutdown.Send);
                    socket.Shutdown(SocketShutdown.Both);
                    //socket.Close();
                }
                catch
                {

                }
            }
        }
        public int Count
        {
            get
            {
                return sockets.Count;
            }
        }
    }

    [Serializable]
    public class Server
    {
        public string server;
        public int server_port;
        public int server_udp_port;
        public string password;
        public string method;
        public string obfs;
        public string obfsparam;
        public string remarks_base64;
        public string group;
        public bool udp_over_tcp;
        public string protocol;
        public bool enable;
        public string id;

        private object protocoldata;
        private object obfsdata;
        private ServerSpeedLog serverSpeedLog = new ServerSpeedLog();
        private DnsBuffer dnsBuffer = new DnsBuffer();
        private DnsBuffer dnsTargetBuffer = new DnsBuffer();
        private Connections Connections = new Connections();

        public void CopyServer(Server Server)
        {
            this.serverSpeedLog = Server.serverSpeedLog;
            this.dnsBuffer = Server.dnsBuffer;
            this.dnsTargetBuffer = Server.dnsTargetBuffer;
            this.Connections = Server.Connections;
            this.enable = Server.enable;
        }
        public void SetConnections(Connections Connections)
        {
            this.Connections = Connections;
        }

        public Connections GetConnections()
        {
            return Connections;
        }

        public DnsBuffer DnsBuffer()
        {
            return dnsBuffer;
        }

        public DnsBuffer DnsTargetBuffer()
        {
            return dnsTargetBuffer;
        }

        public ServerSpeedLog ServerSpeedLog()
        {
            return serverSpeedLog;
        }
        public void SetServerSpeedLog(ServerSpeedLog log)
        {
            serverSpeedLog = log;
        }
        public string remarks
        {
            get
            {
                if (this.remarks_base64.Length == 0)
                    return this.remarks_base64;
                string remarks = this.remarks_base64.Replace('-', '+').Replace('_', '/');
                try
                {
                    return Encoding.UTF8.GetString(System.Convert.FromBase64String(remarks));
                }
                catch (FormatException)
                {
                    remarks = remarks_base64;
                    remarks_base64 = Util.Utils.EncodeUrlSafeBase64(remarks_base64);
                    return remarks;
                }
            }
            set
            {
                remarks_base64 = Util.Utils.EncodeUrlSafeBase64(value);
            }
        }
        public string FriendlyName()
        {
            if (string.IsNullOrEmpty(server))
            {
                return I18N.GetString("New server");
            }
            if (string.IsNullOrEmpty(remarks_base64))
            {
                if (server.IndexOf(':') >= 0)
                {
                    return "[" + server + "]:" + server_port;
                }
                else
                {
                    return server + ":" + server_port;
                }
            }
            else
            {
                if (server.IndexOf(':') >= 0)
                {
                    return remarks + " ([" + server + "]:" + server_port + ")";
                }
                else
                {
                    return remarks + " (" + server + ":" + server_port + ")";
                }
            }
        }

        public Server Clone()
        {
            Server ret = new Server();
            ret.server = (string)server.Clone();
            ret.server_port = server_port;
            ret.password = (string)password.Clone();
            ret.method = (string)method.Clone();
            ret.protocol = protocol;
            ret.obfs = (string)obfs.Clone();
            ret.obfsparam = (string)obfsparam.Clone();
            ret.remarks_base64 = (string)remarks_base64.Clone();
            ret.enable = enable;
            ret.udp_over_tcp = udp_over_tcp;
            ret.id = id;
            ret.protocoldata = protocoldata;
            ret.obfsdata = obfsdata;
            return ret;
        }

        public Server()
        {
            this.server = "server ip or url";
            this.server_port = 8388;
            this.method = "aes-256-cfb";
            this.obfs = "plain";
            this.obfsparam = "";
            this.password = "0";
            this.remarks_base64 = "";
            this.udp_over_tcp = false;
            this.protocol = "origin";
            this.enable = true;
            byte[] id = new byte[16];
            Util.Utils.RandBytes(id, id.Length);
            this.id = BitConverter.ToString(id).Replace("-", "");
        }

        public Server(string ssURL) : this()
        {
            if (ssURL.StartsWith("ss://"))
            {
                ServerFromSS(ssURL);
            }
            else if (ssURL.StartsWith("ssr://"))
            {
                ServerFromSSR(ssURL);
            }
            else
            {
                throw new FormatException();
            }
        }

        public static string DecodeBase64(string val)
        {
            if (val.LastIndexOf(':') > 0)
            {
                return val;
            }
            else
            {
                return Util.Utils.DecodeBase64(val);
            }
        }

        public static string DecodeUrlSafeBase64(string val)
        {
            if (val.LastIndexOf(':') > 0)
            {
                return val;
            }
            else
            {
                return Util.Utils.DecodeUrlSafeBase64(val);
            }
        }

        public void ServerFromSSR(string ssrURL)
        {
            // ssr://host:port:protocol:method:obfs:base64pass/?obfsparam=base64&remarks=base64&group=base64&udpport=0&uot=1
            string[] r1 = Regex.Split(ssrURL, "ssr://", RegexOptions.IgnoreCase);
            string base64 = r1[1].ToString();
            string data = DecodeUrlSafeBase64(base64);
            string param_str = "";
            Dictionary<string, string> params_dict = new Dictionary<string, string>();

            if (data.Length == 0)
            {
                throw new FormatException();
            }
            int param_start_pos = data.IndexOf("?");
            if (param_start_pos > 0)
            {
                param_str = data.Substring(param_start_pos + 1);
                data = data.Substring(0, param_start_pos);

                string[] obfs_params = param_str.Split('&');
                foreach (string p in obfs_params)
                {
                    if (p.IndexOf('=') > 0)
                    {
                        int index = p.IndexOf('=');
                        string key, val;
                        key = p.Substring(0, index);
                        val = p.Substring(index + 1);
                        if (key == "obfsparam" || key == "remarks" || key == "group")
                        {
                            string new_val = DecodeUrlSafeBase64(val);
                            val = new_val;
                        }
                        params_dict[key] = val;
                    }
                }
            }
            if (data.IndexOf("/") >= 0)
            {
                data = data.Substring(0, data.LastIndexOf("/"));
            }

            string[] main_info = data.Split(new char[] { ':' }, StringSplitOptions.None);
            if (main_info.Length > 6)
            {
                string[] main_info_ = new string[6];
                main_info_[5] = main_info[main_info.Length - 1];
                main_info_[4] = main_info[main_info.Length - 2];
                main_info_[3] = main_info[main_info.Length - 3];
                main_info_[2] = main_info[main_info.Length - 4];
                main_info_[1] = main_info[main_info.Length - 5];
                main_info_[0] = main_info[main_info.Length - 6];
                for (int i = main_info.Length - 7; i >= 0; --i)
                {
                    main_info_[0] = main_info[i] + ":" + main_info_[0];
                }
                main_info = main_info_;
            }
            if (main_info.Length != 6)
            {
                throw new FormatException();
            }
            this.server = main_info[0];
            this.server_port = int.Parse(main_info[1]);
            this.protocol = main_info[2].Length == 0 ? "origin" : main_info[2];
            this.protocol.Replace("_compatible", "");
            this.method = main_info[3];
            this.obfs = main_info[4].Length == 0 ? "plain" : main_info[4];
            this.obfs.Replace("_compatible", "");
            this.password = DecodeUrlSafeBase64(main_info[5]);

            if (params_dict.ContainsKey("obfsparam"))
            {
                this.obfsparam = params_dict["obfsparam"];
            }
            if (params_dict.ContainsKey("remarks"))
            {
                this.remarks = params_dict["remarks"];
            }
            if (params_dict.ContainsKey("group"))
            {
                this.group = params_dict["group"];
            }
            if (params_dict.ContainsKey("uot"))
            {
                this.udp_over_tcp = int.Parse(params_dict["uot"]) != 0;
            }
            if (params_dict.ContainsKey("udpport"))
            {
                this.server_udp_port = int.Parse(params_dict["udpport"]);
            }
        }

        public void ServerFromSS(string ssURL)
        {
            // ss://obfs:protocol:method:passwd@host:port/#remarks
            string[] r1 = Regex.Split(ssURL, "ss://", RegexOptions.IgnoreCase);
            string base64 = r1[1].ToString();
            string data = DecodeBase64(base64);
            if (data.Length == 0)
            {
                throw new FormatException();
            }
            try
            {
                int indexLastAt = data.LastIndexOf('@');
                int remarkIndexLastAt = data.IndexOf('#', indexLastAt);
                if (remarkIndexLastAt > 0)
                {
                    if (remarkIndexLastAt + 1 < data.Length)
                    {
                        this.remarks_base64 = data.Substring(remarkIndexLastAt + 1);
                    }
                    data = data.Substring(0, remarkIndexLastAt);
                }
                remarkIndexLastAt = data.IndexOf('/', indexLastAt);
                string param = "";
                if (remarkIndexLastAt > 0)
                {
                    if (remarkIndexLastAt + 1 < data.Length)
                    {
                        param = data.Substring(remarkIndexLastAt + 1);
                    }
                    data = data.Substring(0, remarkIndexLastAt);
                }
                //int paramIndexLastAt = param.IndexOf('?', indexLastAt);
                //Dictionary<string, string> params_dict = new Dictionary<string, string>();
                //if (paramIndexLastAt >= 0)
                //{
                //    string[] obfs_params = param.Substring(paramIndexLastAt + 1).Split('&');
                //    foreach (string p in obfs_params)
                //    {
                //        if (p.IndexOf('=') > 0)
                //        {
                //            int index = p.IndexOf('=');
                //            string key, val;
                //            key = p.Substring(0, index);
                //            val = p.Substring(index + 1);
                //            try
                //            {
                //                byte[] b64_bytes = System.Convert.FromBase64String(val);
                //                if (b64_bytes != null)
                //                {
                //                    val = Encoding.UTF8.GetString(b64_bytes);
                //                }
                //            }
                //            catch (FormatException)
                //            {
                //                continue;
                //            }
                //            params_dict[key] = val;
                //        }
                //    }
                //}

                string afterAt = data.Substring(indexLastAt + 1);
                int indexLastColon = afterAt.LastIndexOf(':');
                this.server_port = int.Parse(afterAt.Substring(indexLastColon + 1));
                this.server = afterAt.Substring(0, indexLastColon);

                string beforeAt = data.Substring(0, indexLastAt);
                this.method = "";
                for (bool next = true; next;)
                {
                    string[] parts = beforeAt.Split(new[] { ':' }, 2);
                    if (parts.Length > 1)
                    {
                        try
                        {
                            Obfs.ObfsBase obfs = (Obfs.ObfsBase)Obfs.ObfsFactory.GetObfs(parts[0]);
                            if (obfs.GetObfs().ContainsKey(parts[0]))
                            {
                                int[] p = obfs.GetObfs()[parts[0]];
                                if (p[0] == 1)
                                {
                                    this.protocol = parts[0];
                                }
                                if (p[1] == 1)
                                {
                                    this.obfs = parts[0];
                                }
                            }
                            else
                            {
                                next = false;
                            }
                        }
                        catch
                        {
                            try
                            {
                                IEncryptor encryptor = EncryptorFactory.GetEncryptor(parts[0], "m");
                                encryptor.Dispose();
                                this.method = parts[0];
                                beforeAt = parts[1];
                            }
                            catch
                            {
                            }
                            break;
                        }
                        beforeAt = parts[1];
                    }
                    else
                        break;
                }
                if (this.method.Length == 0)
                    throw new FormatException();
                this.password = beforeAt;
                //if (params_dict.ContainsKey("obfs"))
                //{
                //    this.obfsparam = params_dict["obfs"];
                //}
            }
            catch (IndexOutOfRangeException)
            {
                throw new FormatException();
            }
        }

        public bool isEnable()
        {
            return enable;
        }

        public void setEnable(bool enable)
        {
            this.enable = enable;
        }

        public object getObfsData()
        {
            return this.obfsdata;
        }
        public void setObfsData(object data)
        {
            this.obfsdata = data;
        }

        public object getProtocolData()
        {
            return this.protocoldata;
        }
        public void setProtocolData(object data)
        {
            this.protocoldata = data;
        }
    }
}
