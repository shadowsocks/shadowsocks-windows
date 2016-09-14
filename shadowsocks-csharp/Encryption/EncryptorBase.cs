using System.Text;

namespace Shadowsocks.Encryption
{
    public struct EncryptorInfo
    {
        public string name;
        public int key_size;
        public int iv_size;
        public int type;

        public EncryptorInfo(string name, int key_size, int iv_size, int type)
        {
            this.name = name;
            this.key_size = key_size;
            this.iv_size = iv_size;
            this.type = type;
        }
    }

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
