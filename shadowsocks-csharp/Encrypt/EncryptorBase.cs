using System.Security.Cryptography;
using System.Text;

namespace Shadowsocks.Encrypt
{
    public abstract class EncryptorBase
        : IEncryptor
    {
        protected EncryptorBase(string method, string password)
        {
            Method = method;
            Password = password;
        }

        protected string Method;
        protected string Password;

        protected byte[] GetPasswordHash()
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(Password);
            byte[] hash = MD5.Create().ComputeHash(inputBytes);
            return hash;
        }

        public abstract void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength);

        public abstract void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength);

        public abstract void Dispose();
    }
}
