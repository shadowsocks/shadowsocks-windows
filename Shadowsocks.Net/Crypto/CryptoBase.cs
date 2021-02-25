using System;

namespace Shadowsocks.Net.Crypto
{
    public abstract class CryptoBase : ICrypto
    {
        private static int _currentId = 0;

        public const int MaxInputSize = 32768;

        public const int MAX_DOMAIN_LEN = 255;
        public const int ADDR_PORT_LEN = 2;
        public const int ADDR_ATYP_LEN = 1;

        public const int ATYP_IPv4 = 0x01;
        public const int ATYP_DOMAIN = 0x03;
        public const int ATYP_IPv6 = 0x04;

        public const int MD5Length = 16;

        // for debugging only, give it a number to trace data stream
        public readonly int instanceId;

        protected CryptoBase(string method, string password)
        {
            instanceId = _currentId;
            _currentId++;

            Method = method;
            Password = password;
        }

        protected string Method;
        protected string Password;

        public override string ToString()
        {
            return $"{instanceId}({Method},{Password})";
        }

        public abstract int Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher);
        public abstract int Decrypt(Span<byte> plain, ReadOnlySpan<byte> cipher);
        public abstract int EncryptUDP(ReadOnlySpan<byte> plain, Span<byte> cipher);
        public abstract int DecryptUDP(Span<byte> plain, ReadOnlySpan<byte> cipher);

        public int AddressBufferLength { get; set; } = -1;

        public abstract void Dispose();
    }
}
