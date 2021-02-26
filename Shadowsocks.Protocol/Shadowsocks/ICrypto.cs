using System;

namespace Shadowsocks.Protocol.Shadowsocks
{
    // stream cipher simply ignore nonce
    public interface ICrypto : IDisposable
    {
        void Init(byte[] key, byte[] iv);
        int Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plain, Span<byte> cipher);
        int Decrypt(ReadOnlySpan<byte> nonce, Span<byte> plain, ReadOnlySpan<byte> cipher);
    }
}
