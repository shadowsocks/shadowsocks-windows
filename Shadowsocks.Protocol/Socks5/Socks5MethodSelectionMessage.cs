using System;

namespace Shadowsocks.Protocol.Socks5
{
    public class Socks5MethodSelectionMessage : Socks5Message
    {
        // 5 auth
        public byte SelectedAuth;

        public override int Serialize(Memory<byte> buffer)
        {
            if (buffer.Length < 2) throw Util.BufferTooSmall(2, buffer.Length, nameof(buffer));

            buffer.Span[0] = 5;
            buffer.Span[1] = SelectedAuth;
            return 2;
        }

        public override (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            // need 3 byte
            if (buffer.Length < 2) return (false, 2);
            if (buffer.Span[0] != 5) return (false, 0);

            SelectedAuth = buffer.Span[1];
            return (true, 2);
        }

        public override bool Equals(IProtocolMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return SelectedAuth == ((Socks5MethodSelectionMessage) other).SelectedAuth;
        }
    }
}