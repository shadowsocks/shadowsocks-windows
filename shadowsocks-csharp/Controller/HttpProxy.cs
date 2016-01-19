using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Shadowsocks.Controller
{
    class HttpPraser
    {
        public bool httpProxy = false;
        public byte[] httpRequestBuffer;
        public int httpContentLength = 0;
        //public byte[] lastContentBuffer;
        public string httpAuthUser;
        public string httpAuthPass;
        protected string httpHost;
        protected int httpPort;
        bool redir;

        public HttpPraser(bool redir = false)
        {
            this.redir = redir;
        }

        private static string ParseHostAndPort(string str, ref int port)
        {
            string host;
            if (str.StartsWith("["))
            {
                int pos = str.LastIndexOf(']');
                if (pos > 0)
                {
                    host = str.Substring(1, pos - 1);
                    if (str.Length > pos + 1 && str[pos + 2] == ':')
                    {
                        port = Convert.ToInt32(str.Substring(pos + 2));
                    }
                }
                else
                {
                    host = str;
                }
            }
            else
            {
                int pos = str.LastIndexOf(':');
                if (pos > 0)
                {
                    host = str.Substring(0, pos);
                    port = Convert.ToInt32(str.Substring(pos + 1));
                }
                else
                {
                    host = str;
                }
            }
            return host;
        }

        protected string ParseURL(string url, string host, int port)
        {
            if (url.StartsWith("http://"))
            {
                url = url.Substring(7);
            }
            if (url.StartsWith("["))
            {
                if (url.StartsWith("[" + host + "]"))
                {
                    url = url.Substring(host.Length + 2);
                }
            }
            else if (url.StartsWith(host))
            {
                url = url.Substring(host.Length);
            }
            if (url.StartsWith(":"))
            {
                if (url.StartsWith(":" + port.ToString()))
                {
                    url = url.Substring((":" + port.ToString()).Length);
                }
            }
            if (!url.StartsWith("/"))
            {
                int pos_slash = url.IndexOf('/');
                int pos_space = url.IndexOf(' ');
                if (pos_slash > 0 && pos_slash < pos_space)
                {
                    url = url.Substring(pos_slash);
                }
            }
            if (url.StartsWith(" "))
            {
                url = "/" + url;
            }
            return url;
        }

        public void HostToHandshakeBuffer(string host, int port, ref byte[] remoteHeaderSendBuffer)
        {
            if (redir)
            {
                remoteHeaderSendBuffer = new byte[0];
            }
            else if (host.Length > 0)
            {
                IPAddress ipAddress;
                bool parsed = IPAddress.TryParse(host, out ipAddress);
                if (!parsed)
                {
                    remoteHeaderSendBuffer = new byte[2 + host.Length + 2];
                    remoteHeaderSendBuffer[0] = 3;
                    remoteHeaderSendBuffer[1] = (byte)host.Length;
                    System.Text.Encoding.UTF8.GetBytes(host).CopyTo(remoteHeaderSendBuffer, 2);
                }
                else
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        remoteHeaderSendBuffer = new byte[7];
                        remoteHeaderSendBuffer[0] = 1;
                        ipAddress.GetAddressBytes().CopyTo(remoteHeaderSendBuffer, 1);
                    }
                    else
                    {
                        remoteHeaderSendBuffer = new byte[19];
                        remoteHeaderSendBuffer[0] = 4;
                        ipAddress.GetAddressBytes().CopyTo(remoteHeaderSendBuffer, 1);
                    }
                }
                remoteHeaderSendBuffer[remoteHeaderSendBuffer.Length - 2] = (byte)(port >> 8);
                remoteHeaderSendBuffer[remoteHeaderSendBuffer.Length - 1] = (byte)(port & 0xff);
            }
        }

        protected int AppendRequest(ref byte[] Packet, ref int PacketLength)
        {
            if (httpContentLength > 0)
            {
                if (httpContentLength >= PacketLength)
                {
                    httpContentLength -= PacketLength;
                    PacketLength = 0;
                    Packet = new byte[0];
                    return -1;
                }
                else
                {
                    int len = PacketLength - httpContentLength;
                    byte[] nextbuffer = new byte[len];
                    Array.Copy(Packet, httpContentLength, nextbuffer, 0, len);
                    Packet = nextbuffer;
                    PacketLength -= httpContentLength;
                    httpContentLength = 0;
                }
            }
            byte[] block = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
            int pos;
            if (httpRequestBuffer == null)
            {
                httpRequestBuffer = new byte[PacketLength];
            }
            else
            {
                Array.Resize(ref httpRequestBuffer, httpRequestBuffer.Length + PacketLength);
            }
            Array.Copy(Packet, 0, httpRequestBuffer, httpRequestBuffer.Length - PacketLength, PacketLength);
            pos = Util.Utils.FindStr(httpRequestBuffer, httpRequestBuffer.Length, block);
            return pos;
        }

        protected Dictionary<string, string> ParseHttpHeader(string header)
        {
            Dictionary<string, string> header_dict = new Dictionary<string, string>();
            string[] lines = header.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] cmdItems = lines[0].Split(new[] { ' ' }, 3);
            for (int index = 1; index < lines.Length; ++index)
            {
                string[] parts = lines[index].Split(new string[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    header_dict[parts[0]] = parts[1];
                }
            }
            header_dict["@0"] = cmdItems[0];
            header_dict["@1"] = cmdItems[1];
            header_dict["@2"] = cmdItems[2];
            return header_dict;
        }

        protected string HeaderDictToString(Dictionary<string, string> dict)
        {
            string cmd = "";
            string result = "";
            cmd = dict["@0"] + " " + dict["@1"] + " " + dict["@2"] + "\r\n";
            dict.Remove("@0");
            dict.Remove("@1");
            dict.Remove("@2");
            result += "Host" + ": " + dict["Host"] + "\r\n";
            dict.Remove("Host");
            foreach (KeyValuePair<string, string> it in dict)
            {
                result += it.Key + ": " + it.Value + "\r\n";
            }
            return cmd + result + "\r\n";
        }

        public int HandshakeReceive(byte[] _firstPacket, int _firstPacketLength, ref byte[] remoteHeaderSendBuffer)
        {
            remoteHeaderSendBuffer = null;
            int pos = AppendRequest(ref _firstPacket, ref _firstPacketLength);
            if (pos < 0)
            {
                return 1;
            }
            string data = System.Text.Encoding.UTF8.GetString(httpRequestBuffer, 0, pos + 4);
            {
                byte[] nextbuffer = new byte[httpRequestBuffer.Length - (pos + 4)];
                Array.Copy(httpRequestBuffer, pos + 4, nextbuffer, 0, nextbuffer.Length);
                httpRequestBuffer = nextbuffer;
            }
            Dictionary<string, string> header_dict = ParseHttpHeader(data);
            this.httpPort = 80;
            if (header_dict["@0"] == "CONNECT")
            {
                this.httpHost = ParseHostAndPort(header_dict["@1"], ref this.httpPort);
            }
            else if (header_dict.ContainsKey("Host"))
            {
                this.httpHost = ParseHostAndPort(header_dict["Host"], ref this.httpPort);
            }
            else
            {
                return 500;
            }
            if (header_dict.ContainsKey("Content-Length") && Convert.ToInt32(header_dict["Content-Length"]) > 0)
            {
                httpContentLength = Convert.ToInt32(header_dict["Content-Length"]) + 2;
            }
            HostToHandshakeBuffer(this.httpHost, this.httpPort, ref remoteHeaderSendBuffer);
            if (redir)
            {
                if (header_dict.ContainsKey("Proxy-Connection"))
                {
                    header_dict["Connection"] = header_dict["Proxy-Connection"];
                    header_dict.Remove("Proxy-Connection");
                }
                string httpRequest = HeaderDictToString(header_dict);
                int len = remoteHeaderSendBuffer.Length;
                byte[] httpData = System.Text.Encoding.UTF8.GetBytes(httpRequest);
                Array.Resize(ref remoteHeaderSendBuffer, len + httpData.Length);
                httpData.CopyTo(remoteHeaderSendBuffer, len);
                httpProxy = true;
            }
            bool auth_ok = false;
            if (httpAuthUser == null || httpAuthUser.Length == 0)
            {
                auth_ok = true;
            }
            if (header_dict.ContainsKey("Proxy-Authorization"))
            {
                string authString = header_dict["Proxy-Authorization"].Substring("Basic ".Length);
                string authStr = httpAuthUser + ":" + (httpAuthPass ?? "");
                string httpAuthString = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(authStr));
                if (httpAuthString == authString)
                {
                    auth_ok = true;
                }
                header_dict.Remove("Proxy-Authorization");
            }
            if (auth_ok && httpRequestBuffer.Length > 0)
            {
                int len = httpRequestBuffer.Length;
                byte[] httpData = httpRequestBuffer;
                Array.Resize(ref remoteHeaderSendBuffer, len + remoteHeaderSendBuffer.Length);
                httpData.CopyTo(remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length - len);
                httpRequestBuffer = new byte[0];
            }
            if (auth_ok && httpContentLength > 0)
            {
                int len = Math.Min(httpRequestBuffer.Length, httpContentLength);
                Array.Resize(ref remoteHeaderSendBuffer, len + remoteHeaderSendBuffer.Length);
                Array.Copy(httpRequestBuffer, 0, remoteHeaderSendBuffer, remoteHeaderSendBuffer.Length - len, len);
                byte[] nextbuffer = new byte[httpRequestBuffer.Length - len];
                Array.Copy(httpRequestBuffer, len, nextbuffer, 0, nextbuffer.Length);
                httpRequestBuffer = nextbuffer;
            }
            else
            {
                httpContentLength = 0;
                httpRequestBuffer = new byte[0];
            }
            if (remoteHeaderSendBuffer == null || !auth_ok)
            {
                return 2;
            }
            if (httpProxy)
            {
                return 3;
            }
            return 0;
        }

        public string Http200()
        {
            return "HTTP/1.1 200 Connection Established\r\n\r\n";
        }

        public string Http407()
        {
            string header = "HTTP/1.1 407 Proxy Authentication Required\r\nProxy-Authenticate: Basic realm=\"RRR\"\r\n";
            string content = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN" +
                        " \"http://www.w3.org/TR/1999/REC-html401-19991224/loose.dtd\">" +
                        "<HTML>" +
                        "  <HEAD>" +
                        "    <TITLE>Error</TITLE>" +
                        "    <META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=ISO-8859-1\">" +
                        "  </HEAD>" +
                        "  <BODY><H1>407 Proxy Authentication Required.</H1></BODY>" +
                        "</HTML>\r\n";
            return header + "\r\n" + content + "\r\n";
        }

        public string Http500()
        {
            string header = "HTTP/1.1 500 Internal Server Error\r\n";
            string content = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN" +
                        " \"http://www.w3.org/TR/1999/REC-html401-19991224/loose.dtd\">" +
                        "<HTML>" +
                        "  <HEAD>" +
                        "    <TITLE>Error</TITLE>" +
                        "    <META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=ISO-8859-1\">" +
                        "  </HEAD>" +
                        "  <BODY><H1>500 Internal Server Error.</H1></BODY>" +
                        "</HTML>";
            return header + "\r\n" + content + "\r\n";
        }
    }
}
