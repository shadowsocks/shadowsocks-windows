using System;
using System.Security.Cryptography;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    class AeadAesGcmCrypto : ICrypto
    {
        AesGcm aes;

        CryptoParameter parameter;
        public AeadAesGcmCrypto(CryptoParameter parameter)
        {
            this.parameter = parameter;
        }

        public void Init(byte[] key, byte[] iv) => aes = new AesGcm(key);

        public int Decrypt(ReadOnlySpan<byte> nonce, Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            aes.Decrypt(
                nonce,
                cipher[0..^parameter.TagSize],
                cipher[^parameter.TagSize..],
                plain[0..(cipher.Length - parameter.TagSize)]);
            return cipher.Length - parameter.TagSize;
        }

        public int Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            aes.Encrypt(
                nonce,
                plain,
                cipher[0..plain.Length],
                cipher.Slice(plain.Length, parameter.TagSize));
            return plain.Length + parameter.TagSize;
        }
    }
}
