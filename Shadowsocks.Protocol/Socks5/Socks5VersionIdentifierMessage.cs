using System;

namespace Shadowsocks.Protocol.Socks5
{
    public class Socks5VersionIdentifierMessage : Socks5Message
    {
        // 5 lAuth [Auth]

        public Memory<byte> Auth;

        public override int Serialize(Memory<byte> buffer)
        {
            var required = Auth.Length + 2;
            if (buffer.Length < required) throw Util.BufferTooSmall(required, buffer.Length, nameof(buffer));

            buffer.Span[0] = 5;
            buffer.Span[1] = (byte) Auth.Length;
            Auth.CopyTo(buffer.Slice(2));
            return Auth.Length + 2;
        }

        public override (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            // need 3 byte
            if (buffer.Length < 3) return (false, 3);
            if (buffer.Span[0] != 5) return (false, 0);
            if (buffer.Span[1] == 0) return (false, 0);
            if (buffer.Length < buffer.Span[1] + 2) return (false, buffer.Span[1] + 2);

            Auth = Util.GetArray(buffer[2..(2 + buffer.Span[1])]);
            return (true, buffer.Span[1] + 2);
        }

        public override bool Equals(IProtocolMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Auth.SequenceEqual(((Socks5VersionIdentifierMessage) other).Auth);
        }
    }
}