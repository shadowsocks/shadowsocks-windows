using System;
using System.Diagnostics;
using System.Net;

namespace Shadowsocks.Protocol.Socks5
{
    public class Socks5UdpMessage : Socks5Message
    {
        public byte Fragment;
        public EndPoint EndPoint = new IPEndPoint(IPAddress.Any, 0);

        public override int Serialize(Memory<byte> buffer)
        {
            var addrLen = NeededBytes(EndPoint);
            if (buffer.Length < addrLen + 3) throw Util.BufferTooSmall(addrLen + 3, buffer.Length, nameof(buffer));
            buffer.Span[0] = 0;
            buffer.Span[1] = 0;
            buffer.Span[2] = Fragment;

            return SerializeAddress(buffer[3..], EndPoint) + 3;
        }

        public override (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 4) return (false, 4);
            if (buffer.Span[0] != 0 || buffer.Span[1] != 0) return (false, 0);
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

            Fragment = buffer.Span[2];
            EndPoint = ep;
            return (true, req);
        }

        public override bool Equals(IProtocolMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            var msg = (Socks5UdpMessage) other;
            return Fragment == msg.Fragment && Equals(EndPoint, msg.EndPoint);
        }
    }
}