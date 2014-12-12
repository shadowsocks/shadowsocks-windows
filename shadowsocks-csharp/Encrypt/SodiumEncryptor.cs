using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Encrypt
{
    public class SodiumEncryptor
        : IVEncryptor, IDisposable
    {
        const int CIPHER_SALSA20 = 1;
        const int CIPHER_CHACHA20 = 2;

        protected uint _encryptBytesRemaining;
        protected uint _decryptBytesRemaining;
        protected ulong _encryptIC;
        protected ulong _decryptIC;

        public SodiumEncryptor(string method, string password)
            : base(method, password)
        {
            InitKey(method, password);
        }

        protected override Dictionary<string, int[]> getCiphers()
        {
            return new Dictionary<string, int[]> {
                {"salsa20", new int[]{32, 8, CIPHER_SALSA20, PolarSSL.AES_CTX_SIZE}},
                {"chacha20", new int[]{32, 8, CIPHER_CHACHA20, PolarSSL.AES_CTX_SIZE}},
            }; ;
        }

        protected override void cipherUpdate(bool isCipher, int length, byte[] buf, byte[] outbuf)
        {
            uint bytesRemaining;
            ulong ic;
            if (isCipher)
            {
                bytesRemaining = _encryptBytesRemaining;
                ic = _encryptIC;
            }
            else
            {
                bytesRemaining = _decryptBytesRemaining;
                ic = _decryptIC;
            }

            if (isCipher)
            {
                _encryptBytesRemaining = bytesRemaining;
                _encryptIC = ic;
            }
            else
            {
                _decryptBytesRemaining = bytesRemaining;
                _decryptIC = ic;
            }
        }

        public override void Dispose()
        {
        }
    }
}
