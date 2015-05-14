using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Shadowsocks.Controller
{
    public class AsyncSocketUserToken
    {
        protected SocketAsyncEventArgs m_receiveEventArgs;
        public SocketAsyncEventArgs ReceiveEventArgs { get { return m_receiveEventArgs; } set { m_receiveEventArgs = value; } }
        protected byte[] m_asyncReceiveBuffer;

        protected Socket m_connectSocket;
        public Socket ConnectSocket
        {
            get
            {
                return m_connectSocket;
            }
            set
            {
                m_connectSocket = value;          
                m_receiveEventArgs.AcceptSocket = m_connectSocket;
            }
        }

        public AsyncSocketUserToken(int asyncReceiveBufferSize)
        {
            m_connectSocket = null;
            m_receiveEventArgs = new SocketAsyncEventArgs();
            m_receiveEventArgs.UserToken = this;
            m_asyncReceiveBuffer = new byte[asyncReceiveBufferSize];
            m_receiveEventArgs.SetBuffer(m_asyncReceiveBuffer, 0, m_asyncReceiveBuffer.Length);
        }
    }
}
