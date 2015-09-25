using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Encryption
{
    public interface IEncryptor : IDisposable
    {
        void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength, bool udp);
        void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength);
    }
}
