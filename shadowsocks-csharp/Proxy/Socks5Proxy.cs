using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Shadowsocks.Controller;
using Shadowsocks.Util.Sockets;

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

        private WrappedSocket _remote = new WrappedSocket();

        private const int Socks5PktMaxSize = 4 + 16 + 2;
        private readonly byte[] _receiveBuffer = new byte[Socks5PktMaxSize];

        public EndPoint LocalEndPoint => _remote.LocalEndPoint;
        public EndPoint ProxyEndPoint { get; private set; }
        public EndPoint DestEndPoint { get; private set; }

        public void BeginConnectProxy(EndPoint remoteEP, AsyncCallback callback, object state)
        {
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

        public void BeginConnectDest(EndPoint destEndPoint, AsyncCallback callback, object state)
        {
            DestEndPoint = destEndPoint;

            byte[] request = null;
            byte atyp = 0;
            int port;

            var dep = destEndPoint as DnsEndPoint;
            if (dep != null)
            {
                // is a domain name, we will leave it to server

                atyp = 3; // DOMAINNAME
                var enc = Encoding.UTF8;
                var hostByteCount = enc.GetByteCount(dep.Host);

                request = new byte[4 + 1/*length byte*/ + hostByteCount + 2];
                request[4] = (byte)hostByteCount;
                enc.GetBytes(dep.Host, 0, dep.Host.Length, request, 5);

                port = dep.Port;
            }
            else
            {
                switch (DestEndPoint.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        request = new byte[4 + 4 + 2];
                        atyp = 1; // IP V4 address
                        break;
                    case AddressFamily.InterNetworkV6:
                        request = new byte[4 + 16 + 2];
                        atyp = 4; // IP V6 address
                        break;
                    default:
                        throw new Exception(I18N.GetString("Proxy request failed"));
                }
                port = ((IPEndPoint) DestEndPoint).Port;
                var addr = ((IPEndPoint)DestEndPoint).Address.GetAddressBytes();
                Array.Copy(addr, 0, request, 4, request.Length - 4 - 2);
            }

            // 构造request包剩余部分
            request[0] = 5;
            request[1] = 1;
            request[2] = 0;
            request[3] = atyp;
            request[request.Length - 2] = (byte) ((port >> 8) & 0xff);
            request[request.Length - 1] = (byte) (port & 0xff);

            var st = new Socks5State();
            st.Callback = callback;
            st.AsyncState = state;

            _remote?.BeginSend(request, 0, request.Length, 0, Socks5RequestSendCallback, st);
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
            _remote?.BeginSend(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            return _remote.EndSend(asyncResult);
        }

        public void BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback,
            object state)
        {
            _remote?.BeginReceive(buffer, offset, size, socketFlags, callback, state);
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
            _remote?.Dispose();
        }
        

        private void ConnectCallback(IAsyncResult ar)
        {
            var state = (Socks5State) ar.AsyncState;
            try
            {
                _remote.EndConnect(ar);

                _remote.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

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
                        ex = new Exception(I18N.GetString("Proxy handshake failed"));
                    }
                }
                else
                {
                    ex = new Exception(I18N.GetString("Proxy handshake failed"));
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
                                state.ex = new Exception(I18N.GetString("Proxy request failed"));
                                state.Callback?.Invoke(new FakeAsyncResult(ar, state));
                                break;
                        }
                    }
                    else
                    {
                        state.ex = new Exception(I18N.GetString("Proxy request failed"));
                        state.Callback?.Invoke(new FakeAsyncResult(ar, state));
                    }
                }
                else
                {
                    state.ex = new Exception(I18N.GetString("Proxy request failed"));
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
                    ex = new Exception(I18N.GetString("Proxy request failed"));
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
