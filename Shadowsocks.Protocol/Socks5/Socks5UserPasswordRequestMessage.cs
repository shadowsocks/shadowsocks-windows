using System;

namespace Shadowsocks.Protocol.Socks5
{
    public class Socks5UserPasswordRequestMessage : Socks5Message
    {
        // 1 lUser [User] lPassword [Password]

        public Memory<byte> User;
        public Memory<byte> Password;

        public override int Serialize(Memory<byte> buffer)
        {
            var required = User.Length + Password.Length + 3;
            if (buffer.Length < required) throw Util.BufferTooSmall(required, buffer.Length, nameof(buffer));
            buffer.Span[0] = 1;
            buffer.Span[1] = (byte) User.Length;
            User.CopyTo(buffer.Slice(2));
            buffer.Span[User.Length + 2] = (byte) Password.Length;
            Password.CopyTo(buffer.Slice(User.Length + 3));
            return required;
        }

        public override (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < 2) return (false, 2);
            if (buffer.Span[0] != 1) return (false, 0);
            int userLength = buffer.Span[1];
            if (buffer.Length < userLength + 3) return (false, userLength + 3);
            int passLength = buffer.Span[userLength + 2];
            if (buffer.Length < userLength + passLength + 3) return (false, userLength + passLength + 3);

            User = Util.GetArray(buffer[2..(2 + userLength)]);
            Password = Util.GetArray(buffer[(3 + userLength)..(3 + userLength + passLength)]);
            return (true, userLength + passLength + 3);
        }

        public override bool Equals(IProtocolMessage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            var msg = (Socks5UserPasswordRequestMessage) other;
            return User.SequenceEqual(msg.User) && Password.SequenceEqual(msg.Password);
        }
    }
}