using CryptoBase.Abstractions.SymmetricCryptos;
using System;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    public abstract class AeadCrypto : ICrypto
    {
        protected IAEADCrypto? crypto;

        private readonly CryptoParameter _parameter;

        protected AeadCrypto(CryptoParameter parameter)
        {
            _parameter = parameter;
        }

        public abstract void Init(byte[] key, byte[] iv);

        public int Decrypt(ReadOnlySpan<byte> nonce, Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            crypto!.Decrypt(
                nonce,
                cipher[..^_parameter.TagSize],
                cipher[^_parameter.TagSize..],
                plain[..(cipher.Length - _parameter.TagSize)]);
            return cipher.Length - _parameter.TagSize;
        }

        public int Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            crypto!.Encrypt(
                nonce,
                plain,
                cipher.Slice(0, plain.Length),
                cipher.Slice(plain.Length, _parameter.TagSize));
            return plain.Length + _parameter.TagSize;
        }

        public void Dispose() => crypto?.Dispose();
    }
}
