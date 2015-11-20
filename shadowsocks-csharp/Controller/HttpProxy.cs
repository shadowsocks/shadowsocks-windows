using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Shadowsocks.Controller
{
    class HttpProxyState
    {
        public bool httpProxy = false;
        public byte[] httpRequestBuffer;
        public int httpContentLength = 0;
        public string httpAuthString;
        protected bool hostChange;
        protected string httpHost;

        private static string ParseHostAndPort(string str, out int port)
        {
            string host;
            port = 80;
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

        protected void ParseHttpRequestHeader(string header, ref string[] lines, out string host, out int port, out string cmd)
        {
            lines = header.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] cmdItems = lines[0].Split(new[] { ' ' }, 2);
            string hostLine = cmdItems[1];
            hostLine = hostLine.Split(' ')[0];
            host = "";
            port = 80;
            cmd = "";
            if (cmdItems[0] == "CONNECT")
            {
                host = ParseHostAndPort(hostLine, out port);
            }
            else
            {
                foreach (string line in lines)
                {
                    if (line.StartsWith("Host: "))
                    {
                        host = line.Substring(6);
                        if (httpHost != host)
                        {
                            if (httpHost != null)
                            {
                                hostChange = true;
                            }
                            else
                            {
                                hostChange = false;
                            }
                            httpHost = host;
                        }
                    }
                    if (line.StartsWith("Content-Length: "))
                    {
                        string len = line.Substring("Content-Length: ".Length);
                        httpContentLength = Convert.ToInt32(len) + 2;  // 2 bytes of CRLF
                    }
                }
                if (host.Length == 0)
                    return;
                host = ParseHostAndPort(host, out port);
                cmdItems[1] = ParseURL(cmdItems[1], host, port);
            }
            cmd = cmdItems[0] + " " + cmdItems[1] + "\r\n";
        }

        public void HostToHandshakeBuffer(string host, int port, ref byte[] remoteHeaderSendBuffer)
        {
            if (host.Length > 0)
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

        public int HandshakeReceive(byte[] _firstPacket, int _firstPacketLength, ref byte[] remoteHeaderSendBuffer)
        {
            remoteHeaderSendBuffer = null;
            byte[] block = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
            int pos;
            if (httpRequestBuffer == null)
                httpRequestBuffer = new byte[_firstPacketLength];
            else
            {
                Array.Resize(ref httpRequestBuffer, httpRequestBuffer.Length + _firstPacketLength);
            }
            Array.Copy(_firstPacket, 0, httpRequestBuffer, httpRequestBuffer.Length - _firstPacketLength, _firstPacketLength);
            pos = Util.Utils.FindStr(httpRequestBuffer, httpRequestBuffer.Length, block);
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
            string[] dataParts = data.Split(new string[] { "\r\n\r\n" }, 2, StringSplitOptions.RemoveEmptyEntries);
            string header = dataParts[0];
            httpProxy = false;
            httpContentLength = 0;

            string[] lines = null;
            string host;
            int port;
            string cmd;
            ParseHttpRequestHeader(header, ref lines, out host, out port, out cmd);

            HostToHandshakeBuffer(host, port, ref remoteHeaderSendBuffer);
            if (!cmd.StartsWith("CONNECT"))
            {
                string httpRequest = cmd + data.Split(new string[] { "\r\n" }, 2, StringSplitOptions.RemoveEmptyEntries)[1];
                httpRequest = httpRequest.Replace("\r\nProxy-Connection: ", "\r\nConnection: ");
                int len = remoteHeaderSendBuffer.Length;
                byte[] httpData = System.Text.Encoding.UTF8.GetBytes(httpRequest);
                Array.Resize(ref remoteHeaderSendBuffer, len + httpData.Length);
                httpData.CopyTo(remoteHeaderSendBuffer, len);
                httpProxy = true;
                Logging.Log(LogLevel.Debug, httpRequest);
            }
            bool auth_ok = false;
            if (httpAuthString == null || httpAuthString.Length == 0)
            {
                auth_ok = true;
            }
            if (!auth_ok)
            {
                foreach (string line in lines)
                {
                    if (line.StartsWith("Proxy-Authorization: Basic "))
                    {
                        if (httpAuthString == line.Substring("Proxy-Authorization: Basic ".Length))
                        {
                            auth_ok = true;
                            break;
                        }
                    }
                }
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

        private string ParseHttpRequest(byte[] buffer, int bufferLen)
        {
            string httpRequest = "";
            string data = System.Text.Encoding.UTF8.GetString(buffer);
            string[] dataParts = data.Split(new string[] { "\r\n\r\n" }, 2, StringSplitOptions.RemoveEmptyEntries);
            string header = dataParts[0];

            string[] lines = null;
            string host;
            int port;
            string cmd;
            ParseHttpRequestHeader(header, ref lines, out host, out port, out cmd);

            httpRequest = cmd + data.Split(new string[] { "\r\n" }, 2, StringSplitOptions.RemoveEmptyEntries)[1];
            httpRequest = httpRequest.Replace("\r\nProxy-Connection: ", "\r\nConnection: ");
            Logging.Log(LogLevel.Info, httpRequest);
            return httpRequest;
        }

        public void ParseHttpRequest(byte[] connetionRecvBuffer, ref int bytesRead)
        {
            byte[] buffer = new byte[bytesRead];
            byte[] block = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
            Array.Copy(connetionRecvBuffer, buffer, bytesRead);
            int pos;
            int outpos = 0;
            while (true)
            {
                if (httpContentLength > 0)
                {
                    if (buffer.Length <= httpContentLength)
                    {
                        buffer.CopyTo(connetionRecvBuffer, outpos);
                        outpos += buffer.Length;
                        bytesRead = outpos;
                        httpContentLength -= buffer.Length;
                        buffer = new byte[0];
                    }
                    else
                    {
                        Array.Copy(buffer, 0, connetionRecvBuffer, outpos, httpContentLength);
                        outpos += httpContentLength;
                        bytesRead = outpos;
                        byte[] nextbuffer = new byte[buffer.Length - httpContentLength];
                        Array.Copy(buffer, httpContentLength, nextbuffer, 0, nextbuffer.Length);
                        buffer = nextbuffer;
                        httpContentLength = 0;
                    }
                }
                if ((pos = Util.Utils.FindStr(buffer, buffer.Length, block)) >= 0)
                {
                    if (httpRequestBuffer == null)
                        httpRequestBuffer = new byte[pos + 4];
                    else
                    {
                        Array.Resize(ref httpRequestBuffer, httpRequestBuffer.Length + pos + 4);
                    }
                    Array.Copy(buffer, 0, httpRequestBuffer, httpRequestBuffer.Length - (pos + 4), pos + 4);
                    string req = ParseHttpRequest(httpRequestBuffer, httpRequestBuffer.Length);
                    if (req.Length > 0)
                    {
                        byte[] buf = System.Text.Encoding.UTF8.GetBytes(req);
                        buf.CopyTo(connetionRecvBuffer, outpos);
                        outpos += buf.Length;
                        bytesRead = outpos;
                    }
                    else
                    {
                        break;
                    }
                    byte[] nextbuffer = new byte[buffer.Length - (pos + 4)];
                    Array.Copy(buffer, pos + 4, nextbuffer, 0, nextbuffer.Length);
                    buffer = nextbuffer;
                }
                else
                {
                    break;
                }
            }
            httpRequestBuffer = buffer;
        }
    }
}
