using System;

namespace Shadowsocks.Encryption
{
    public interface IEncryptor : IDisposable
    {
        /* length == -1 means not used */
        int AddrBufLength { set; get; }
        void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength);
        void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength);
        void EncryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength);
        void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength);
    }
}
