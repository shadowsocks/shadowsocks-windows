using Shadowsocks.Protocol.Shadowsocks.Crypto;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shadowsocks.Protocol.Shadowsocks
{
    public class SaltMessage : IProtocolMessage
    {
        private readonly int length;
        public Memory<byte> Salt { get; private set; }

        public SaltMessage(int length, bool roll = false)
        {
            this.length = length;
            if (roll)
            {
                Salt = new byte[length];
                CryptoUtils.RandomSpan(Salt.Span);
            }
        }

        public bool Equals([AllowNull] IProtocolMessage other) => throw new NotImplementedException();

        public int Serialize(Memory<byte> buffer)
        {
            Salt.CopyTo(buffer);
            return length;
        }

        public (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            if (buffer.Length < length) return (false, length);
            buffer.Slice(0, length).CopyTo(Salt);
            return (true, length);
        }
    }
}
