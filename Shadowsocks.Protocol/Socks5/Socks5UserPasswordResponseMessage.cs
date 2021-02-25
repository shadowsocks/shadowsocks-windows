using System;

namespace Shadowsocks.Protocol.Socks5
{
    public class Socks5UserPasswordResponseMessage : Socks5Message
    {
        // 1 success

        public bool Success;

        public override int Serialize(Memory<byte> buffer)
        {
            if (buffer.Length < 2) throw Util.BufferTooSmall(2, buffer.Length, nameof(buffer));

            buffer.Span[0] = 1;
            buffer.Span[1] = (byte) (Success ? 0 : 1);
            return 2;
        }

        public override (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 2) return (false, 2);
            if (buffer.Span[0] != 1) return (false, 0);

            Success = buffer.Span[1] == 0;
            return (true, 2);
        }

        public override bool Equals(IProtocolMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Success == ((Socks5UserPasswordResponseMessage)other).Success;
        }
    }
}