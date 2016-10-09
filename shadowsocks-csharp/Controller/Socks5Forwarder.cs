using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenDNS;
using Shadowsocks.Model;

namespace Shadowsocks.Controller
{
    class Socks5Forwarder : Listener.Service
    {
        Configuration _config;

        public Socks5Forwarder(Configuration config)
        {
            this._config = config;
        }

        public bool Handle(byte[] firstPacket, int length, Socket socket)
        {
            if (length >= 7)
            {
                if (firstPacket[0] == 1)
                {
                    byte[] addr = new byte[4];
                    Array.Copy(firstPacket, 1, addr, 0, addr.Length);
                    IPAddress ipAddress = new IPAddress(addr);
                    if (_config.proxyRuleMode == 1 && Util.Utils.isLAN(ipAddress))
                    {
                        new Handler().Start(_config, firstPacket, length, socket);
                        return true;
                    }
                }
                else if (firstPacket[0] == 3)
                {
                    int len = firstPacket[1];
                    byte[] addr = new byte[len];
                    if (length >= len + 2)
                    {
                        Array.Copy(firstPacket, 2, addr, 0, addr.Length);
                        string host = Encoding.UTF8.GetString(firstPacket, 2, len);
                        IPAddress ipAddress;
                        if (IPAddress.TryParse(host, out ipAddress))
                        {
                            if (_config.proxyRuleMode == 1 && Util.Utils.isLAN(ipAddress))
                            {
                                new Handler().Start(_config, firstPacket, length, socket);
                                return true;
                            }
                        }
                        else
                        {
                            if (_config.proxyRuleMode == 1 && host.ToLower() == "localhost")
                            {
                                new Handler().Start(_config, firstPacket, length, socket);
                                return true;
                            }
                        }
                    }
                }
                else if (firstPacket[0] == 4)
                {
                    byte[] addr = new byte[16];
                    Array.Copy(firstPacket, 1, addr, 0, addr.Length);
                    IPAddress ipAddress = new IPAddress(addr);
                    if (_config.proxyRuleMode == 1 && Util.Utils.isLAN(ipAddress))
                    {
                        new Handler().Start(_config, firstPacket, length, socket);
                        return true;
                    }
                }
            }
            return false;
        }

        class Handler
        {
            private delegate IPHostEntry GetHostEntryHandler(string ip);

            private byte[] _firstPacket;
            private int _firstPacketLength;
            private Socket _local;
            private Socket _remote;
            private bool _closed = false;
            private Configuration _config;
            public const int RecvSize = 4096;
            // remote receive buffer
            private byte[] remoteRecvBuffer = new byte[RecvSize];
            // connection receive buffer
            private byte[] connetionRecvBuffer = new byte[RecvSize];

            public void Start(Configuration config, byte[] firstPacket, int length, Socket socket)
            {
                _firstPacket = firstPacket;
                _firstPacketLength = length;
                _local = socket;
                _config = config;
                Connect();
            }

            private void StartConnect()
            {
                try
                {
                    Connect();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }

            }

            private IPAddress QueryDns(string host, string dns_servers)
            {
                IPAddress ipAddress;
                bool parsed = IPAddress.TryParse(host, out ipAddress);
                if (!parsed)
                {
                    //if (server.DnsBuffer().isExpired(host))
                    {
                        if (dns_servers != null && dns_servers.Length > 0)
                        {
                            OpenDNS.Types[] types;
                            //if (false)
                            //    types = new Types[] { Types.AAAA, Types.A };
                            //else
                            types = new Types[] { Types.A, Types.AAAA };
                            string[] dns_server = dns_servers.Split(',');
                            for (int query_i = 0; query_i < types.Length; ++query_i)
                            {
                                DnsQuery dns = new DnsQuery(host, types[query_i]);
                                dns.RecursionDesired = true;
                                foreach (string server in dns_server)
                                {
                                    dns.Servers.Add(server);
                                }
                                if (dns.Send())
                                {
                                    int count = dns.Response.Answers.Count;
                                    if (count > 0)
                                    {
                                        for (int i = 0; i < count; ++i)
                                        {
                                            if (((ResourceRecord)dns.Response.Answers[i]).Type != types[query_i])
                                                continue;
                                            return ((OpenDNS.Address)dns.Response.Answers[i]).IP;
                                        }
                                    }
                                }
                            }
                        }
                        {
                            try
                            {
                                GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
                                IAsyncResult result = callback.BeginInvoke(host, null, null);
                                if (result.AsyncWaitHandle.WaitOne(5, false))
                                {
                                    foreach (IPAddress ad in callback.EndInvoke(result).AddressList)
                                    {
                                        return ad;
                                        //if (ad.AddressFamily == AddressFamily.InterNetwork)
                                        //{
                                        //    return ad;
                                        //}
                                    }
                                }
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                return ipAddress;
            }

            private void Connect()
            {
                try
                {
                    IPAddress ipAddress;
                    int _targetPort;
                    if (_firstPacket[0] == 1)
                    {
                        byte[] addr = new byte[4];
                        Array.Copy(_firstPacket, 1, addr, 0, addr.Length);
                        ipAddress = new IPAddress(addr);
                        _targetPort = (_firstPacket[5] << 8) | _firstPacket[6];
                    }
                    else if (_firstPacket[0] == 4)
                    {
                        byte[] addr = new byte[16];
                        Array.Copy(_firstPacket, 1, addr, 0, addr.Length);
                        ipAddress = new IPAddress(addr);
                        _targetPort = (_firstPacket[17] << 8) | _firstPacket[18];
                    }
                    else //if (_firstPacket[0] == 3)
                    {
                        int len = _firstPacket[1];
                        byte[] addr = new byte[len];
                        Array.Copy(_firstPacket, 2, addr, 0, addr.Length);
                        ipAddress = QueryDns(Encoding.UTF8.GetString(_firstPacket, 2, len), _config.dns_server);
                        _targetPort = (_firstPacket[len + 2] << 8) | _firstPacket[len + 3];
                    }
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
                    _remote.EndConnect(ar);
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
                    _remote.BeginReceive(remoteRecvBuffer, 0, RecvSize, 0,
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
                        _remote.BeginSend(connetionRecvBuffer, 0, bytesRead, 0, new AsyncCallback(PipeRemoteSendCallback), null);
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
                    _local.BeginReceive(this.connetionRecvBuffer, 0, RecvSize, 0,
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
                    _remote.BeginReceive(this.remoteRecvBuffer, 0, RecvSize, 0,
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
