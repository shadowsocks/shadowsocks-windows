using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Util.Sockets;

namespace Shadowsocks.Controller.Service
{
    class Http2Socks5 : Listener.Service
    {
        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Tcp)
            {
                return false;
            }

            return true;
        }
    }
}
