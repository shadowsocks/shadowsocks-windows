using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption
{
    public interface IEncryptor
    {
        /* length == -1 means not used */
        int AddressBufferLength { set; get; }
        void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength);
        void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength);
        void EncryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength);
        void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength);
        int Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher);
        int Decrypt(Span<byte> plain, ReadOnlySpan<byte> cipher);
        int EncryptUDP(ReadOnlySpan<byte> plain, Span<byte> cipher);
        int DecryptUDP(Span<byte> plain, ReadOnlySpan<byte> cipher);
    }
}
