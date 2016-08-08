using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Shadowsocks.Controller;

namespace Shadowsocks.Proxy
{
    public class Socks5Proxy : IProxy
    {
        private class FakeAsyncResult : IAsyncResult
        {
            public readonly Socks5State innerState;

            private readonly IAsyncResult r;

            public FakeAsyncResult(IAsyncResult orig, Socks5State state)
            {
                r = orig;
                innerState = state;
            }

            public bool IsCompleted => r.IsCompleted;
            public WaitHandle AsyncWaitHandle => r.AsyncWaitHandle;
            public object AsyncState => innerState.AsyncState;
            public bool CompletedSynchronously => r.CompletedSynchronously;
        }

        private class Socks5State
        {
            public AsyncCallback Callback { get; set; }

            public object AsyncState { get; set; }

            public int BytesToRead;

            public Exception ex { get; set; }
        }

        private Socket _remote;

        private const int Socks5PktMaxSize = 4 + 16 + 2;
        private readonly byte[] _receiveBuffer = new byte[Socks5PktMaxSize];

        public EndPoint LocalEndPoint => _remote.LocalEndPoint;
        public EndPoint ProxyEndPoint { get; private set; }
        public EndPoint DestEndPoint { get; private set; }

        public void BeginConnectProxy(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            _remote = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            var st = new Socks5State();
            st.Callback = callback;
            st.AsyncState = state;

            ProxyEndPoint = remoteEP;

            _remote.BeginConnect(remoteEP, ConnectCallback, st);
        }

        public void EndConnectProxy(IAsyncResult asyncResult)
        {
            var state = ((FakeAsyncResult)asyncResult).innerState;

            if (state.ex != null)
            {
                throw state.ex;
            }
        }

        public void BeginConnectDest(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            var ep = remoteEP as IPEndPoint;
            if (ep == null)
            {
                throw new Exception(I18N.GetString("Proxy request faild"));
            }

            byte[] request = null;
            byte atyp = 0;
            switch (ep.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    request = new byte[4 + 4 + 2];
                    atyp = 1;
                    break;
                case AddressFamily.InterNetworkV6:
                    request = new byte[4 + 16 + 2];
                    atyp = 4;
                    break;
            }
            if (request == null)
            {
                throw new Exception(I18N.GetString("Proxy request faild"));
            }

            // 构造request包
            var addr = ep.Address.GetAddressBytes();
            request[0] = 5;
            request[1] = 1;
            request[2] = 0;
            request[3] = atyp;
            Array.Copy(addr, 0, request, 4, request.Length - 4 - 2);
            request[request.Length - 2] = (byte) ((ep.Port >> 8) & 0xff);
            request[request.Length - 1] = (byte) (ep.Port & 0xff);

            var st = new Socks5State();
            st.Callback = callback;
            st.AsyncState = state;

            DestEndPoint = remoteEP;

            _remote.BeginSend(request, 0, request.Length, 0, Socks5RequestSendCallback, st);

        }

        public void EndConnectDest(IAsyncResult asyncResult)
        {
            var state = ((FakeAsyncResult)asyncResult).innerState;

            if (state.ex != null)
            {
                throw state.ex;
            }
        }

        public void BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state)
        {
            _remote.BeginSend(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            return _remote.EndSend(asyncResult);
        }

        public void BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state)
        {
            _remote.BeginReceive(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            return _remote.EndReceive(asyncResult);
        }

        public void Shutdown(SocketShutdown how)
        {
            _remote?.Shutdown(how);
        }

        public void Close()
        {
            _remote?.Close();
        }
        

        private void ConnectCallback(IAsyncResult ar)
        {
            var state = (Socks5State) ar.AsyncState;
            try
            {
                _remote.EndConnect(ar);

                byte[] handshake = {5, 1, 0};
                _remote.BeginSend(handshake, 0, handshake.Length, 0, Socks5HandshakeSendCallback, state);
            }
            catch (Exception ex)
            {
                state.ex = ex;
                state.Callback?.Invoke(new FakeAsyncResult(ar, state));
            }
        }

        private void Socks5HandshakeSendCallback(IAsyncResult ar)
        {
            var state = (Socks5State)ar.AsyncState;
            try
            {
                _remote.EndSend(ar);

                _remote.BeginReceive(_receiveBuffer, 0, 2, 0, Socks5HandshakeReceiveCallback, state);
            }
            catch (Exception ex)
            {
                state.ex = ex;
                state.Callback?.Invoke(new FakeAsyncResult(ar, state));
            }
        }

        private void Socks5HandshakeReceiveCallback(IAsyncResult ar)
        {
            Exception ex = null;
            var state = (Socks5State)ar.AsyncState;
            try
            {
                var bytesRead = _remote.EndReceive(ar);
                if (bytesRead >= 2)
                {
                    if (_receiveBuffer[0] != 5 || _receiveBuffer[1] != 0)
                    {
                        ex = new Exception(I18N.GetString("Proxy handshake faild"));
                    }
                }
                else
                {
                    ex = new Exception(I18N.GetString("Proxy handshake faild"));
                }
            }
            catch (Exception ex2)
            {
                ex = ex2;
            }
            state.ex = ex;
            state.Callback?.Invoke(new FakeAsyncResult(ar, state));
        }


        private void Socks5RequestSendCallback(IAsyncResult ar)
        {
            var state = (Socks5State)ar.AsyncState;
            try
            {
                _remote.EndSend(ar);

                _remote.BeginReceive(_receiveBuffer, 0, 4, 0, Socks5ReplyReceiveCallback, state);
            }
            catch (Exception ex)
            {
                state.ex = ex;
                state.Callback?.Invoke(new FakeAsyncResult(ar, state));
            }
        }

        private void Socks5ReplyReceiveCallback(IAsyncResult ar)
        {
            var state = (Socks5State)ar.AsyncState;
            try
            {
                var bytesRead = _remote.EndReceive(ar);
                if (bytesRead >= 4)
                {
                    if (_receiveBuffer[0] == 5 && _receiveBuffer[1] == 0)
                    {
                        // 跳过剩下的reply
                        switch (_receiveBuffer[3]) // atyp
                        {
                            case 1:
                                state.BytesToRead = 4 + 2;
                                _remote.BeginReceive(_receiveBuffer, 0, 4 + 2, 0, Socks5ReplyReceiveCallback2, state);
                                break;
                            case 4:
                                state.BytesToRead = 16 + 2;
                                _remote.BeginReceive(_receiveBuffer, 0, 16 + 2, 0, Socks5ReplyReceiveCallback2, state);
                                break;
                            default:
                                state.ex = new Exception(I18N.GetString("Proxy request faild"));
                                state.Callback?.Invoke(new FakeAsyncResult(ar, state));
                                break;
                        }
                    }
                    else
                    {
                        state.ex = new Exception(I18N.GetString("Proxy request faild"));
                        state.Callback?.Invoke(new FakeAsyncResult(ar, state));
                    }
                }
                else
                {
                    state.ex = new Exception(I18N.GetString("Proxy request faild"));
                    state.Callback?.Invoke(new FakeAsyncResult(ar, state));
                }
            }
            catch (Exception ex)
            {
                state.ex = ex;
                state.Callback?.Invoke(new FakeAsyncResult(ar, state));
            }
        }


        private void Socks5ReplyReceiveCallback2(IAsyncResult ar)
        {
            Exception ex = null;
            var state = (Socks5State)ar.AsyncState;
            try
            {
                var bytesRead = _remote.EndReceive(ar);
                var bytesNeedSkip = state.BytesToRead;

                if (bytesRead < bytesNeedSkip)
                {
                    ex = new Exception(I18N.GetString("Proxy request faild"));
                }
            }
            catch (Exception ex2)
            {
                ex = ex2;
            }

            state.ex = ex;
            state.Callback?.Invoke(new FakeAsyncResult(ar, state));
        }
    }
}
