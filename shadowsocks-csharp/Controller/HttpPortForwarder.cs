using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    class HttpPortForwarder : Listener.Service
    {
        int _targetPort;
        Configuration _config;

        public HttpPortForwarder(int targetPort, Configuration config)
        {
            this._targetPort = targetPort;
            this._config = config;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket)
        {
            new Handler().Start(_config, firstPacket, length, socket, this._targetPort);
            return true;
        }

        class Handler
        {
            private byte[] _firstPacket;
            private int _firstPacketLength;
            private int _targetPort;
            private Socket _local;
            private Socket _remote;
            private bool _closed = false;
            private bool _localShutdown = false;
            private bool _remoteShutdown = false;
            private Configuration _config;
            HttpPraser httpProxyState;
            public const int RecvSize = 16384;
            // remote receive buffer
            private byte[] remoteRecvBuffer = new byte[RecvSize];
            // connection receive buffer
            private byte[] connetionRecvBuffer = new byte[RecvSize];

            public void Start(Configuration config, byte[] firstPacket, int length, Socket socket, int targetPort)
            {
                this._firstPacket = firstPacket;
                this._firstPacketLength = length;
                this._local = socket;
                this._targetPort = targetPort;
                this._config = config;
                if ((_config.authUser ?? "").Length == 0 || Util.Utils.isMatchSubNet(((IPEndPoint)this._local.RemoteEndPoint).Address, "127.0.0.0/8"))
                {
                    Connect();
                }
                else
                {
                    RspHttpHandshakeReceive();
                }
            }
            private void RspHttpHandshakeReceive()
            {
                if (httpProxyState == null)
                {
                    httpProxyState = new HttpPraser(true);
                }
                httpProxyState.httpAuthUser = _config.authUser;
                httpProxyState.httpAuthPass = _config.authPass;
                byte[] remoteHeaderSendBuffer = null;
                int err = httpProxyState.HandshakeReceive(_firstPacket, _firstPacketLength, ref remoteHeaderSendBuffer);
                if (err == 1)
                {
                    _local.BeginReceive(connetionRecvBuffer, 0, _firstPacket.Length, 0,
                        new AsyncCallback(HttpHandshakeRecv), null);
                }
                else if (err == 2)
                {
                    string dataSend = httpProxyState.Http407();
                    byte[] httpData = System.Text.Encoding.UTF8.GetBytes(dataSend);
                    _local.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(HttpHandshakeAuthEndSend), null);
                }
                else if (err == 3)
                {
                    Connect();
                }
                else if (err == 4)
                {
                    Connect();
                }
                else if (err == 0)
                {
                    string dataSend = httpProxyState.Http200();
                    byte[] httpData = System.Text.Encoding.UTF8.GetBytes(dataSend);
                    _local.BeginSend(httpData, 0, httpData.Length, 0, new AsyncCallback(StartConnect), null);
                }
            }

            private void HttpHandshakeRecv(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    int bytesRead = _local.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        Array.Copy(connetionRecvBuffer, _firstPacket, bytesRead);
                        _firstPacketLength = bytesRead;
                        RspHttpHandshakeReceive();
                    }
                    else
                    {
                        Console.WriteLine("failed to recv data in HttpHandshakeRecv");
                        this.Close();
                    }
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
                }
            }

            private void HttpHandshakeAuthEndSend(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    _local.EndSend(ar);
                    _local.BeginReceive(connetionRecvBuffer, 0, _firstPacket.Length, 0,
                        new AsyncCallback(HttpHandshakeRecv), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
                }
            }

            private void StartConnect(IAsyncResult ar)
            {
                try
                {
                    _local.EndSend(ar);
                    Connect();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
                }

            }

            private void Connect()
            {
                try
                {
                    // TODO async resolving
                    IPAddress ipAddress;
                    bool parsed = IPAddress.TryParse("127.0.0.1", out ipAddress);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, _targetPort);


                    _remote = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);
                    _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

                    // Connect to the remote endpoint.
                    _remote.BeginConnect(remoteEP,
                        new AsyncCallback(ConnectCallback), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
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
                    _remote.EndConnect(ar);
                    HandshakeReceive();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
                }
            }

            private void HandshakeReceive()
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    _remote.BeginSend(_firstPacket, 0, _firstPacketLength, 0, new AsyncCallback(StartPipe), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
                }
            }


            private void StartPipe(IAsyncResult ar)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    _remote.EndSend(ar);
                    _remote.BeginReceive(remoteRecvBuffer, 0, RecvSize, 0,
                        new AsyncCallback(PipeRemoteReceiveCallback), null);
                    _local.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                        new AsyncCallback(PipeConnectionReceiveCallback), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
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
                    int bytesRead = _remote.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        _local.BeginSend(remoteRecvBuffer, 0, bytesRead, 0, new AsyncCallback(PipeConnectionSendCallback), null);
                    }
                    else
                    {
                        _local.Shutdown(SocketShutdown.Send);
                        _localShutdown = true;
                        CheckClose();
                    }
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
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
                    int bytesRead = _local.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        _remote.BeginSend(connetionRecvBuffer, 0, bytesRead, 0, new AsyncCallback(PipeRemoteSendCallback), null);
                    }
                    else
                    {
                        _remote.Shutdown(SocketShutdown.Send);
                        _remoteShutdown = true;
                        CheckClose();
                    }
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
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
                    _remote.EndSend(ar);
                    _local.BeginReceive(this.connetionRecvBuffer, 0, RecvSize, 0,
                        new AsyncCallback(PipeConnectionReceiveCallback), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
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
                    _local.EndSend(ar);
                    _remote.BeginReceive(this.remoteRecvBuffer, 0, RecvSize, 0,
                        new AsyncCallback(PipeRemoteReceiveCallback), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    this.Close();
                }
            }

            private void CheckClose()
            {
                if (_localShutdown && _remoteShutdown)
                {
                    this.Close();
                }
            }

            public void Close()
            {
                lock (this)
                {
                    if (_closed)
                    {
                        return;
                    }
                    _closed = true;
                }
                if (_local != null)
                {
                    try
                    {
                        _local.Shutdown(SocketShutdown.Both);
                        _local.Close();
                    }
                    catch (Exception e)
                    {
                        Logging.LogUsefulException(e);
                    }
                }
                if (_remote != null)
                {
                    try
                    {
                        _remote.Shutdown(SocketShutdown.Both);
                        _remote.Close();
                    }
                    catch (SocketException e)
                    {
                        Logging.LogUsefulException(e);
                    }
                }
            }
        }
    }
}
