using System.Text;

namespace Shadowsocks.Encryption
{
    public abstract class EncryptorBase
        : IEncryptor
    {
        public const int MAX_INPUT_SIZE = 32768;

        protected EncryptorBase(string method, string password, bool onetimeauth, bool isudp)
        {
            Method = method;
            Password = password;
            OnetimeAuth = onetimeauth;
            IsUDP = isudp;
        }

        protected string Method;
        protected string Password;
        protected bool OnetimeAuth;
        protected bool IsUDP;

        protected byte[] GetPasswordHash()
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(Password);
            byte[] hash = MbedTLS.MD5(inputBytes);
            return hash;
        }

        public abstract void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength);

        public abstract void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength);

        public abstract void Dispose();
    }
}
