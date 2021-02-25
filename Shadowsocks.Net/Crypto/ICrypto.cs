using System;

namespace Shadowsocks.Net.Crypto
{
    public interface ICrypto : IDisposable
    {
        int Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher);
        int Decrypt(Span<byte> plain, ReadOnlySpan<byte> cipher);
        int EncryptUDP(ReadOnlySpan<byte> plain, Span<byte> cipher);
        int DecryptUDP(Span<byte> plain, ReadOnlySpan<byte> cipher);
    }
}
