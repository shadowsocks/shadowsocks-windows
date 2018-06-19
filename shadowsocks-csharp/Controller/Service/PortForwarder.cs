using System;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Util.Sockets;

namespace Shadowsocks.Controller
{
    class PortForwarder : Listener.Service
    {
        private readonly int _targetPort;

        public PortForwarder(int targetPort)
        {
            _targetPort = targetPort;
        }

        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
            {
                return false;
            }
            new Handler().Start(firstPacket, length, socket, _targetPort);
            return true;
        }

        private class Handler
        {
            private byte[] _firstPacket;
            private int _firstPacketLength;
            private Socket _local;
            private WrappedSocket _remote;
            private bool _closed = false;
            private bool _localShutdown = false;
            private bool _remoteShutdown = false;
            private const int RecvSize = 2048;
            // remote receive buffer
            private byte[] remoteRecvBuffer = new byte[RecvSize];
            // connection receive buffer
            private byte[] connetionRecvBuffer = new byte[RecvSize];

            // instance-based lock
            private readonly object _Lock = new object();

            public void Start(byte[] firstPacket, int length, Socket socket, int targetPort)
            {
                _firstPacket = firstPacket;
                _firstPacketLength = length;
                _local = socket;
                try
                {
                    EndPoint remoteEP = SocketUtil.GetEndPoint("127.0.0.1", targetPort);

                    // Connect to the remote endpoint.
                    _remote = new WrappedSocket();
                    _remote.BeginConnect(remoteEP, ConnectCallback, null);
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
                    _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                    HandshakeReceive();
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
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
                    _remote.BeginSend(_firstPacket, 0, _firstPacketLength, 0, StartPipe, null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
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
                        PipeRemoteReceiveCallback, null);
                    _local.BeginReceive(connetionRecvBuffer, 0, RecvSize, 0,
                        PipeConnectionReceiveCallback, null);
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
                        _local.BeginSend(remoteRecvBuffer, 0, bytesRead, 0, PipeConnectionSendCallback, null);
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
                        _remote.BeginSend(connetionRecvBuffer, 0, bytesRead, 0, PipeRemoteSendCallback, null);
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
                    _local.EndSend(ar);
                    _remote.BeginReceive(remoteRecvBuffer, 0, RecvSize, 0,
                        PipeRemoteReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    Close();
                }
            }

            private void CheckClose()
            {
                if (_localShutdown && _remoteShutdown)
                {
                    Close();
                }
            }

            public void Close()
            {
                lock (_Lock)
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
                        _remote.Dispose();
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
