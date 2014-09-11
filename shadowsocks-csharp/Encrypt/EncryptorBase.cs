using System.Security.Cryptography;
using System.Text;

namespace shadowsocks_csharp.Encrypt
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

        public abstract byte[] Encrypt(byte[] buf, int length);

        public abstract byte[] Decrypt(byte[] buf, int length);
    }
}
