using System;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    class UnsafeNoneCrypto : ICrypto
    {
        public UnsafeNoneCrypto(CryptoParameter parameter)
        {
        }

        public int Decrypt(ReadOnlySpan<byte> nonce, Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            cipher.CopyTo(plain);
            return plain.Length;
        }

        public int Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            plain.CopyTo(cipher);
            return plain.Length;
        }

        public void Init(byte[] key, byte[] iv)
        {
        }

        public void Dispose()
        {
        }
    }
}
