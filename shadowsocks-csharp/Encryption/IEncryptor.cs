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
    }
}
