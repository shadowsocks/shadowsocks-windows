using System;
using System.Diagnostics;
using System.Net;

namespace Shadowsocks.Protocol.Socks5
{
    public class Socks5RequestReplyMessageBase : Socks5Message
    {
        // 5 cmdOrReply 0 aType [addr] port
        
        protected byte CmdByte;
        public EndPoint EndPoint =new IPEndPoint(IPAddress.Any, 0);

        public override int Serialize(Memory<byte> buffer)
        {
            var addrLen = NeededBytes(EndPoint);
            if (buffer.Length < addrLen + 3) throw Util.BufferTooSmall(addrLen + 3, buffer.Length, nameof(buffer));
            buffer.Span[0] = 5;
            buffer.Span[1] = CmdByte;
            buffer.Span[2] = 0;

            return SerializeAddress(buffer[3..], EndPoint) + 3;
        }

        public override (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 4) return (false, 4);
            if (buffer.Span[0] != 5 || buffer.Span[2] != 0) return (false, 0);
            if (buffer.Span[3] == 3 && buffer.Length < 5) return (false, 5);
            var req = buffer.Span[3] switch
            {
                AddressIPv4 => 10,
                AddressDomain => buffer.Span[4] + 7,
                AddressIPv6 => 22,
                _ => 0,
            };
            if (req == 0) return (false, 0);
            if (buffer.Length < req) return (false, req);

            (var state, var len) = TryParseAddress(buffer[3..], out var ep);
            Debug.Assert(state);
            Debug.Assert(len == req - 3);

            CmdByte = buffer.Span[1];
            EndPoint = ep;
            return (true, req);
        }
        
        public override bool Equals(IProtocolMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false; 
            var msg = (Socks5RequestReplyMessageBase) other;
            return CmdByte == msg.CmdByte && EndPoint.Equals(msg.EndPoint);
        }
    }
    
    public class Socks5ReplyMessage : Socks5RequestReplyMessageBase
    {
        public byte Reply
        {
            get => CmdByte;
            set => CmdByte = value;
        }
    }
    
    public class Socks5RequestMessage : Socks5RequestReplyMessageBase
    {
        public byte Command
        {
            get => CmdByte;
            set => CmdByte = value;
        }
    }
}