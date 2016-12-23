using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenDNS;
using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    class Socks5Forwarder : Listener.Service
    {
        private Configuration _config;
        private IPRangeSet _IPRange;

        public Socks5Forwarder(Configuration config, IPRangeSet IPRange)
        {
            _config = config;
            _IPRange = IPRange;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket)
        {
            int handle = IsHandle(firstPacket, length, socket);
            if (handle > 0)
            {
                if (_config.proxyEnable)
                {
                    new Handler().Start(_config, _IPRange, firstPacket, length, socket, handle == 2);
                }
                else
                {
                    new Handler().Start(_config, _IPRange, firstPacket, length, socket, false);
                }
                return true;
            }
            return false;
        }

        public int IsHandle(byte[] firstPacket, int length, Socket socket)
        {
            if (length >= 7)
            {
                IPAddress ipAddress = null;
                if (firstPacket[0] == 1)
                {
                    byte[] addr = new byte[4];
                    Array.Copy(firstPacket, 1, addr, 0, addr.Length);
                    ipAddress = new IPAddress(addr);
                }
                else if (firstPacket[0] == 3)
                {
                    int len = firstPacket[1];
                    byte[] addr = new byte[len];
                    if (length >= len + 2)
                    {
                        Array.Copy(firstPacket, 2, addr, 0, addr.Length);
                        string host = Encoding.UTF8.GetString(firstPacket, 2, len);
                        if (IPAddress.TryParse(host, out ipAddress))
                        {
                            //pass
                        }
                        else
                        {
                            if (_config.proxyRuleMode != 0
                                && host.ToLower() == "localhost") //TODO: load system host file
                            {
                                return 1;
                            }
                            if (_config.proxyRuleMode == 2 && _IPRange != null)
                            {
                                ipAddress = Utils.DnsBuffer.Get(host);
                                if (ipAddress == null)
                                {
                                    ipAddress = Util.Utils.QueryDns(host, _config.dns_server);
                                    if (ipAddress != null)
                                    {
                                        Utils.DnsBuffer.Set(host, ipAddress);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (firstPacket[0] == 4)
                {
                    byte[] addr = new byte[16];
                    Array.Copy(firstPacket, 1, addr, 0, addr.Length);
                    ipAddress = new IPAddress(addr);
                }
                if (ipAddress != null)
                {
                    if (_config.proxyRuleMode != 0
                        && Util.Utils.isLAN(ipAddress))
                    {
                        return 1;
                    }
                    if (_config.proxyRuleMode == 2 && _IPRange != null
                        && ipAddress.AddressFamily == AddressFamily.InterNetwork
                        )
                    {
                        if (_IPRange.IsInIPRange(ipAddress))
                        {
                            return 2;
                        }
                        Utils.DnsBuffer.Sweep();
                    }
                }
            }
            return 0;
        }

        class Handler
        {
            private IPRangeSet _IPRange;
            private Configuration _config;

            private byte[] _firstPacket;
            private int _firstPacketLength;
            private Socket _local;
            private ProxySocketTun _remote;

            private bool _closed = false;
            private bool _remote_go_proxy = false;
            private string _remote_host;
            private int _remote_port;

            public const int RecvSize = 4096;
            // remote receive buffer
            private byte[] remoteRecvBuffer = new byte[RecvSize];
            // connection receive buffer
            private byte[] connetionRecvBuffer = new byte[RecvSize];

            public void Start(Configuration config, IPRangeSet IPRange, byte[] firstPacket, int length, Socket socket, bool proxy)
            {
                _IPRange = IPRange;
                _firstPacket = firstPacket;
                _firstPacketLength = length;
                _local = socket;
                _config = config;
                _remote_go_proxy = proxy;
                Connect();
            }

            private void Connect()
            {
                try
                {
                    IPAddress ipAddress = null;
                    int _targetPort = 0;
                    {
                        if (_firstPacket[0] == 1)
                        {
                            byte[] addr = new byte[4];
                            Array.Copy(_firstPacket, 1, addr, 0, addr.Length);
                            ipAddress = new IPAddress(addr);
                            _targetPort = (_firstPacket[5] << 8) | _firstPacket[6];
                            _remote_host = ipAddress.ToString();
                            System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString() + "]" + "Direct connect " + _remote_host + ":" + _targetPort.ToString());
                        }
                        else if (_firstPacket[0] == 4)
                        {
                            byte[] addr = new byte[16];
                            Array.Copy(_firstPacket, 1, addr, 0, addr.Length);
                            ipAddress = new IPAddress(addr);
                            _targetPort = (_firstPacket[17] << 8) | _firstPacket[18];
                            _remote_host = ipAddress.ToString();
                            System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString() + "]" + "Direct connect " + _remote_host + ":" + _targetPort.ToString());
                        }
                        else if (_firstPacket[0] == 3)
                        {
                            int len = _firstPacket[1];
                            byte[] addr = new byte[len];
                            Array.Copy(_firstPacket, 2, addr, 0, addr.Length);
                            _remote_host = Encoding.UTF8.GetString(_firstPacket, 2, len);
                            _targetPort = (_firstPacket[len + 2] << 8) | _firstPacket[len + 3];
                            System.Diagnostics.Debug.WriteLine("[" + DateTime.Now.ToString() + "]" + "Direct connect " + _remote_host + ":" + _targetPort.ToString());

                            if (!_remote_go_proxy)
                            {
                                if (!IPAddress.TryParse(_remote_host, out ipAddress))
                                {
                                    ipAddress = Utils.DnsBuffer.Get(_remote_host);
                                }
                                if (ipAddress == null)
                                {
                                    ipAddress = Utils.QueryDns(_remote_host, _config.dns_server);
                                }
                                if (ipAddress != null)
                                {
                                    Utils.DnsBuffer.Set(_remote_host, ipAddress);
                                    Utils.DnsBuffer.Sweep();
                                }
                                else
                                {
                                    throw new SocketException((int)SocketError.HostNotFound);
                                }
                            }
                        }
                        _remote_port = _targetPort;
                    }
                    if (_remote_go_proxy)
                    {
                        IPAddress.TryParse(_config.proxyHost, out ipAddress);
                        _targetPort = _config.proxyPort;
                    }
                    // ProxyAuth recv only socks5 head, so don't need to save anything else
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, _targetPort);

                    _remote = new ProxySocketTun(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);
                    _remote.GetSocket().SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

                    // Connect to the remote endpoint.
                    _remote.BeginConnect(remoteEP,
                        new AsyncCallback(ConnectCallback), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private bool ConnectProxyServer(string strRemoteHost, int iRemotePort)
            {
                if (_config.proxyType == 0)
                {
                    bool ret = _remote.ConnectSocks5ProxyServer(strRemoteHost, iRemotePort, false, _config.proxyAuthUser, _config.proxyAuthPass);
                    return ret;
                }
                else if (_config.proxyType == 1)
                {
                    bool ret = _remote.ConnectHttpProxyServer(strRemoteHost, iRemotePort, _config.proxyAuthUser, _config.proxyAuthPass, _config.proxyUserAgent);
                    return ret;
                }
                else
                {
                    return true;
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
                    if (_remote_go_proxy)
                    {
                        if (!ConnectProxyServer(_remote_host, _remote_port))
                        {
                            throw new SocketException((int)SocketError.ConnectionReset);
                        }
                    }
                    StartPipe();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private void StartPipe()
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    _remote.BeginReceive(remoteRecvBuffer, RecvSize, 0,
                        new AsyncCallback(PipeRemoteReceiveCallback), null);
                    _local.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                        new AsyncCallback(PipeConnectionReceiveCallback), null);
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
                    int bytesRead = _remote.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        _local.BeginSend(remoteRecvBuffer, 0, bytesRead, 0, new AsyncCallback(PipeConnectionSendCallback), null);
                    }
                    else
                    {
                        Close();
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
                    int bytesRead = _local.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        _remote.BeginSend(connetionRecvBuffer, bytesRead, 0, new AsyncCallback(PipeRemoteSendCallback), null);
                    }
                    else
                    {
                        Close();
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
                    _remote.EndSend(ar);
                    _local.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                        new AsyncCallback(PipeConnectionReceiveCallback), null);
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
                    _local.EndSend(ar);
                    _remote.BeginReceive(remoteRecvBuffer, RecvSize, 0,
                        new AsyncCallback(PipeRemoteReceiveCallback), null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
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
                Thread.Sleep(100);
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
            }
        }
    }
}
