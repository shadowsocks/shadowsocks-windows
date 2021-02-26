using Shadowsocks.Protocol.Shadowsocks.Crypto;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shadowsocks.Protocol.Shadowsocks
{
    class AeadBlockMessage : IProtocolMessage
    {
        public Memory<byte> Data;
        private readonly int tagLength;
        private readonly ICrypto aead;
        private Memory<byte> nonce;

        private int expectedDataLength;

        public AeadBlockMessage(ICrypto aead, Memory<byte> nonce, CryptoParameter parameter)
        {
            this.aead = aead;
            this.nonce = nonce;
            tagLength = parameter.TagSize;
        }

        public bool Equals([AllowNull] IProtocolMessage other) => throw new NotImplementedException();

        public int Serialize(Memory<byte> buffer)
        {
            var len = Data.Length + 2 * tagLength + 2;
            if (buffer.Length < len)
                throw Util.BufferTooSmall(len, buffer.Length, nameof(buffer));
            Memory<byte> m = new byte[2];
            m.Span[0] = (byte)(Data.Length / 256);
            m.Span[1] = (byte)(Data.Length % 256);
            var len1 = aead.Encrypt(nonce.Span, m.Span, buffer.Span);
            nonce.Span.SodiumIncrement();
            buffer = buffer.Slice(len1);
            aead.Encrypt(nonce.Span, Data.Span, buffer.Span);
            nonce.Span.SodiumIncrement();
            return len;
        }

        public (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer)
        {
            int len;
            if (expectedDataLength == 0)
            {
                if (buffer.Length < tagLength + 2) return (false, tagLength + 2);

                // decrypt length
                Memory<byte> m = new byte[2];
                len = aead.Decrypt(nonce.Span, m.Span, buffer.Span);
                nonce.Span.SodiumIncrement();
                if (len != 2) return (false, 0);

                expectedDataLength = m.Span[0] * 256 + m.Span[1];
                if (expectedDataLength > 0x3fff) return (false, 0);
            }
            var totalLength = expectedDataLength + 2 * tagLength + 2;
            if (buffer.Length < totalLength) return (false, totalLength);

            // decrypt data
            var dataBuffer = buffer.Slice(tagLength + 2);
            Data = new byte[expectedDataLength];
            len = aead.Decrypt(nonce.Span, Data.Span, dataBuffer.Span);
            nonce.Span.SodiumIncrement();
            if (len != expectedDataLength) return (false, 0);
            return (true, totalLength);
        }
    }
}
