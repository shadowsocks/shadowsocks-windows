using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Shadowsocks.ForwardProxy;
using Shadowsocks.Util.Sockets;

namespace Shadowsocks.Controller.Service
{
    class Http2Socks5 : Listener.Service
    {

        private readonly ByteSearch.SearchTarget _connectSearch =
            new ByteSearch.SearchTarget(Encoding.UTF8.GetBytes("HTTP"));

        private readonly int _socks5Port;

        public Http2Socks5(int socks5Port)
        {
            _socks5Port = socks5Port;
        }

        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
            {
                return false;
            }

            if (_connectSearch.SearchIn(firstPacket, 0, length) != -1)
            {
                new HttpHandler(_socks5Port, firstPacket, length, socket);

                return true;
            }
            return false;
        }

        private class HttpHandler
        {
            private const string HTTP_CRLF = "\r\n";

            private const string HTTP_CONNECT_200 =
                "HTTP/1.1 200 Connection established" + HTTP_CRLF +
                "Proxy-Connection: close" + HTTP_CRLF +
                "Proxy-Agent: Shadowsocks" + HTTP_CRLF +
                "" + HTTP_CRLF; // End with an empty line

            private readonly WrappedSocket _localSocket;
            private readonly int _socks5Port;
            private Socks5Proxy _socks5;


            private bool _closed = false;
            private bool _localShutdown = false;
            private bool _remoteShutdown = false;
            private readonly object _Lock = new object();


            private const int RecvSize = 16384;
            // remote receive buffer
            private readonly byte[] _remoteRecvBuffer = new byte[RecvSize];
            // connection receive buffer
            private readonly byte[] _connetionRecvBuffer = new byte[RecvSize];


            public HttpHandler(int socks5Port, byte[] firstPacket, int length, Socket socket)
            {
                _socks5Port = socks5Port;
                _localSocket = new WrappedSocket(socket);
                _localSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                new LineReader(firstPacket, _localSocket, firstPacket, 0, length, OnLineRead, OnException, OnFinish,
                    Encoding.UTF8, HTTP_CRLF, null);
            }

            private void CheckClose()
            {
                if (_localShutdown && _remoteShutdown)
                {
                    Close();
                }
            }

            private void Close()
            {
                lock (_Lock)
                {
                    if (_closed)
                    {
                        return;
                    }
                    _closed = true;
                }

                _localSocket.Dispose();
                _socks5?.Close();
            }

            private byte[] _lastBytes;
            private int _lastBytesIndex;
            private int _lastBytesLength;

            #region Socks5 Process

            private void ProxyConnectCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }

                try
                {
                    _socks5.EndConnectProxy(ar);

                    _socks5.BeginConnectDest(SocketUtil.GetEndPoint(_targetHost, _targetPort), ConnectCallback, null);
                }
                catch (ArgumentException)
                {
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private void ConnectCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }

                try
                {
                    _socks5.EndConnectDest(ar);

                    if (_isConnect)
                    {
                        // http connect response
                        SendConnectResponse();
                    }
                    else
                    {
                        // send header
                        SendHeader();
                    }
                }
                catch (ArgumentException)
                {
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            #endregion

            #region CONNECT

            private void SendConnectResponse()
            {
                var len = Encoding.UTF8.GetBytes(HTTP_CONNECT_200, 0, HTTP_CONNECT_200.Length, _remoteRecvBuffer, 0);
                _localSocket.BeginSend(_remoteRecvBuffer, 0, len, SocketFlags.None, Http200SendCallback, null);
            }

            private void Http200SendCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }

                try
                {
                    _localSocket.EndSend(ar);

                    StartPipe();
                }
                catch (ArgumentException)
                {
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            #endregion

            #region Other http method except CONNECT

            private void SendHeader()
            {
                var h = _headers.Dequeue() + HTTP_CRLF;
                var len = Encoding.UTF8.GetBytes(h, 0, h.Length, _connetionRecvBuffer, 0);
                _socks5.BeginSend(_connetionRecvBuffer, 0, len, SocketFlags.None, HeaderSendCallback, null);

            }

            private void HeaderSendCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }

                try
                {
                    _socks5.EndSend(ar);

                    if (_headers.Count > 0)
                    {
                        SendHeader();
                    }
                    else
                    {
                        StartPipe();
                    }
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            #endregion

            #region Pipe

            private void StartPipe()
            {
                if (_closed)
                {
                    return;
                }

                try
                {
                    _socks5.BeginReceive(_remoteRecvBuffer, 0, RecvSize, 0,
                        PipeRemoteReceiveCallback, null);

                    if (_lastBytesLength > 0)
                    {
                        _socks5.BeginSend(_lastBytes, _lastBytesIndex, _lastBytesLength, SocketFlags.None,
                            PipeRemoteSendCallback, null);
                    }
                    else
                    {
                        _localSocket.BeginReceive(_connetionRecvBuffer, 0, RecvSize, 0,
                            PipeConnectionReceiveCallback, null);
                    }
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private void PipeRemoteReceiveCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    int bytesRead = _socks5.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        _localSocket.BeginSend(_remoteRecvBuffer, 0, bytesRead, 0, PipeConnectionSendCallback, null);
                    }
                    else
                    {
                        _localSocket.Shutdown(SocketShutdown.Send);
                        _localShutdown = true;
                        CheckClose();
                    }
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private void PipeConnectionReceiveCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    int bytesRead = _localSocket.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        _socks5.BeginSend(_connetionRecvBuffer, 0, bytesRead, 0, PipeRemoteSendCallback, null);
                    }
                    else
                    {
                        _socks5.Shutdown(SocketShutdown.Send);
                        _remoteShutdown = true;
                        CheckClose();
                    }
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private void PipeRemoteSendCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    _socks5.EndSend(ar);
                    _localSocket.BeginReceive(_connetionRecvBuffer, 0, RecvSize, 0,
                        PipeConnectionReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private void PipeConnectionSendCallback(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    _localSocket.EndSend(ar);
                    _socks5.BeginReceive(_remoteRecvBuffer, 0, RecvSize, 0,
                        PipeRemoteReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            #endregion

            #region Header Parse

            private void OnException(Exception ex, object state)
            {
                throw ex;
            }

            private static readonly Regex HttpRequestHeaderRegex = new Regex(@"^([A-Z]+?) ([^\s]+) HTTP/1\.\d$");

            private int _requestLineCount = 0;
            private volatile bool _isConnect = false;

            private string _targetHost;
            private int _targetPort;
            private readonly Queue<string> _headers = new Queue<string>();

            private bool ParseHost(string host)
            {
                var locs = host.Split(':');
                _targetHost = locs[0];
                if (locs.Length > 1)
                {
                    if (!int.TryParse(locs[1], out _targetPort))
                    {
                        return false;
                    }
                }
                else
                {
                    _targetPort = 80;
                }

                return true;
            }

            private bool OnLineRead(string line, object state)
            {
                if (_closed)
                {
                    return true;
                }

                Logging.Debug(line);

                if (!line.StartsWith("Proxy-"))
                {
                    _headers.Enqueue(line);
                }

                if (_requestLineCount == 0)
                {
                    var m = HttpRequestHeaderRegex.Match(line);
                    if (m.Success)
                    {
                        var method = m.Groups[1].Value;

                        if (method == "CONNECT")
                        {
                            _isConnect = true;

                            if (!ParseHost(m.Groups[2].Value))
                            {
                                throw new Exception("Bad http header: " + line);
                            }
                        }
                    }
                }
                else
                {
                    if (line.IsNullOrEmpty())
                    {
                        return true;
                    }

                    if (!_isConnect)
                    {
                        if (line.StartsWith("Host: "))
                        {
                            if (!ParseHost(line.Substring(6).Trim()))
                            {
                                throw new Exception("Bad http header: " + line);
                            }
                        }
                    }
                }

                _requestLineCount++;

                return false;
            }

            private void OnFinish(byte[] lastBytes, int index, int length, object state)
            {
                if (_closed)
                {
                    return;
                }

                if (_targetHost == null)
                {
                    Logging.Error("Unkonwn host");
                    Close();
                }
                else
                {
                    if (length > 0)
                    {
                        _lastBytes = lastBytes;
                        _lastBytesIndex = index;
                        _lastBytesLength = length;
                    }

                    // Start socks5 conn
                    _socks5 = new Socks5Proxy();

                    _socks5.BeginConnectProxy(SocketUtil.GetEndPoint("127.0.0.1", _socks5Port), ProxyConnectCallback,
                        null);
                }
            }

            #endregion

        }
    }
}
