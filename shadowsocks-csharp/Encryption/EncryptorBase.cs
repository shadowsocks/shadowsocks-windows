namespace Shadowsocks.Encryption
{
    public abstract class EncryptorBase : IEncryptor
    {
        private static int _currentId = 0;

        public const int MAX_INPUT_SIZE = 32768;

        public const int MAX_DOMAIN_LEN = 255;
        public const int ADDR_PORT_LEN = 2;
        public const int ADDR_ATYP_LEN = 1;

        public const int ATYP_IPv4 = 0x01;
        public const int ATYP_DOMAIN = 0x03;
        public const int ATYP_IPv6 = 0x04;

        public const int MD5_LEN = 16;

        // for debugging only, give it a number to trace data stream
        public readonly int instanceId;

        protected EncryptorBase(string method, string password)
        {
            instanceId = _currentId;
            _currentId++;

            Method = method;
            Password = password;
        }

        protected string Method;
        protected string Password;

        public abstract void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength);

        public abstract void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength);

        public abstract void EncryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength);

        public abstract void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength);

        public override string ToString()
        {
            return $"{instanceId}({Method},{Password})";
        }

        public int AddressBufferLength { get; set; } = -1;
    }
}