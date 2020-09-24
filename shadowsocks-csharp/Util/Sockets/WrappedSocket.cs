using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Shadowsocks.Util.Sockets
{
    /*
     * A wrapped socket class which support both ipv4 and ipv6 based on the
     * connected remote endpoint.
     * 
     * If the server address is host name, then it may have both ipv4 and ipv6 address
     * after resolving. The main idea is we don't want to resolve and choose the address
     * by ourself. Instead, Socket.ConnectAsync() do handle this thing internally by trying
     * each address and returning an established socket connection.
     */
    public class WrappedSocket
    {
        public EndPoint LocalEndPoint => _activeSocket?.LocalEndPoint;

        // Only used during connection and close, so it won't cost too much.
        private SpinLock _socketSyncLock = new SpinLock();

        private bool _disposed;
        private bool Connected => _activeSocket != null;
        private Socket _activeSocket;


        public void BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (Connected)
            {
                throw new SocketException((int) SocketError.IsConnected);
            }

            var arg = new SocketAsyncEventArgs();
            arg.RemoteEndPoint = remoteEP;
            arg.Completed += OnTcpConnectCompleted;
            arg.UserToken = new TcpUserToken(callback, state);

            if(!Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, arg))
            {
                OnTcpConnectCompleted(this, arg);
            }
        }

        private class FakeAsyncResult : IAsyncResult
        {
            public bool IsCompleted { get; } = true;
            public WaitHandle AsyncWaitHandle { get; } = null;
            public object AsyncState { get; set; }
            public bool CompletedSynchronously { get; } = true;
            public Exception InternalException { get; set; } = null;
        }

        private class TcpUserToken
        {
            public AsyncCallback Callback { get; }
            public object AsyncState { get; }

            public TcpUserToken(AsyncCallback callback, object state)
            {
                Callback = callback;
                AsyncState = state;
            }
        }

        private void OnTcpConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            using (args)
            {
                args.Completed -= OnTcpConnectCompleted;
                var token = (TcpUserToken) args.UserToken;

                if (args.SocketError != SocketError.Success)
                {
                    var ex = args.ConnectByNameError ?? new SocketException((int) args.SocketError);

                    var r = new FakeAsyncResult()
                    {
                        AsyncState = token.AsyncState,
                        InternalException = ex
                    };

                    token.Callback(r);
                }
                else
                {
                    var lockTaken = false;
                    if (!_socketSyncLock.IsHeldByCurrentThread)
                    {
                        _socketSyncLock.TryEnter(ref lockTaken);
                    }
                    try
                    {
                        if (Connected)
                        {
                            args.ConnectSocket.FullClose();
                        }
                        else
                        {
                            _activeSocket = args.ConnectSocket;
                            if (_disposed)
                            {
                                _activeSocket.FullClose();
                            }

                            var r = new FakeAsyncResult()
                            {
                                AsyncState = token.AsyncState
                            };
                            token.Callback(r);
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            _socketSyncLock.Exit();
                        }
                    }
                }
            }
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var r = asyncResult as FakeAsyncResult;
            if (r == null)
            {
                throw new ArgumentException("Invalid asyncResult.", nameof(asyncResult));
            }

            if (r.InternalException != null)
            {
                throw r.InternalException;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            var lockTaken = false;
            if (!_socketSyncLock.IsHeldByCurrentThread)
            {
                _socketSyncLock.TryEnter(ref lockTaken);
            }
            try
            {
                _disposed = true;
                _activeSocket?.FullClose();
            }
            finally
            {
                if (lockTaken)
                {
                    _socketSyncLock.Exit();
                }
            }

        }

        public IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags,
            AsyncCallback callback,
            object state)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!Connected)
            {
                throw new SocketException((int) SocketError.NotConnected);
            }

            return _activeSocket.BeginSend(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!Connected)
            {
                throw new SocketException((int) SocketError.NotConnected);
            }

            return _activeSocket.EndSend(asyncResult);
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags,
            AsyncCallback callback,
            object state)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!Connected)
            {
                throw new SocketException((int) SocketError.NotConnected);
            }

            return _activeSocket.BeginReceive(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!Connected)
            {
                throw new SocketException((int) SocketError.NotConnected);
            }

            return _activeSocket.EndReceive(asyncResult);
        }

        public void Shutdown(SocketShutdown how)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!Connected)
            {
                return;
            }

            _activeSocket.Shutdown(how);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
        {
            SetSocketOption(optionLevel, optionName, optionValue ? 1 : 0);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!Connected)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            _activeSocket.SetSocketOption(optionLevel, optionName, optionValue);
        }
    }
}
