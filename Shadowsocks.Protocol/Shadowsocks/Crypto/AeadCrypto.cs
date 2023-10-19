using CryptoBase.Abstractions.SymmetricCryptos;
using System;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto;

public abstract class AeadCrypto(CryptoParameter parameter) : ICrypto
{
    protected IAEADCrypto? crypto;

    public abstract void Init(byte[] key, byte[] iv);

    public int Decrypt(ReadOnlySpan<byte> nonce, Span<byte> plain, ReadOnlySpan<byte> cipher)
    {
        crypto!.Decrypt(
            nonce,
            cipher[..^parameter.TagSize],
            cipher[^parameter.TagSize..],
            plain[..(cipher.Length - parameter.TagSize)]);
        return cipher.Length - parameter.TagSize;
    }

    public int Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plain, Span<byte> cipher)
    {
        crypto!.Encrypt(
            nonce,
            plain,
            cipher.Slice(0, plain.Length),
            cipher.Slice(plain.Length, parameter.TagSize));
        return plain.Length + parameter.TagSize;
    }

    public void Dispose() => crypto?.Dispose();
}