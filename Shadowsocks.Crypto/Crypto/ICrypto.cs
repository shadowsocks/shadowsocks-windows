using System;

namespace Shadowsocks.Crypto
{
    public interface ICrypto
    {
        int Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher);
        int Decrypt(Span<byte> plain, ReadOnlySpan<byte> cipher);
        int EncryptUDP(ReadOnlySpan<byte> plain, Span<byte> cipher);
        int DecryptUDP(Span<byte> plain, ReadOnlySpan<byte> cipher);
    }
}
