using System;
using System.Collections.Generic;
using System.Text;

namespace shadowsocks_csharp.Encrypt
{
    public interface IEncryptor
    {
        byte[] Encrypt(byte[] buf, int length);
        byte[] Decrypt(byte[] buf, int length);
    }
}
