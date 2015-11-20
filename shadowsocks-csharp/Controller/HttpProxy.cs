using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Shadowsocks.Controller
{
    class HttpProxyState
    {
        public byte[] httpRequestBuffer;

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

        protected bool ParseHttpRequestHeader(string header, ref string[] lines, out string host, out int port, out string cmd)
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
                return false;
            }
            cmd = cmdItems[0] + " " + cmdItems[1] + "\r\n";
            return true;
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

            string[] lines = null;
            string host;
            int port;
            string cmd;
            ParseHttpRequestHeader(header, ref lines, out host, out port, out cmd);

            HostToHandshakeBuffer(host, port, ref remoteHeaderSendBuffer);
            return 0;
        }
    }
}
