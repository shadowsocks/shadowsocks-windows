using System;

namespace UnitTestProject1.Encryption
{
    public interface IEncryptor : IDisposable
    {
        void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength);
        void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength);
    }
}
