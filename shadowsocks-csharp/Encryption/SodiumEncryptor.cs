using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption
{
    public class SodiumEncryptor
        : IVEncryptor, IDisposable
    {
        const int CIPHER_SALSA20 = 1;
        const int CIPHER_CHACHA20 = 2;
        const int CIPHER_CHACHA20_IETF = 3;

        const int SODIUM_BLOCK_SIZE = 64;

        static byte[] sodiumBuf = new byte[MAX_INPUT_SIZE + SODIUM_BLOCK_SIZE];

        protected int _encryptBytesRemaining;
        protected int _decryptBytesRemaining;
        protected ulong _encryptIC;
        protected ulong _decryptIC;

        public SodiumEncryptor(string method, string password, bool onetimeauth, bool isudp)
            : base(method, password, onetimeauth, isudp)
        {
        }

        private static Dictionary<string, Dictionary<string, int[]>> _ciphers = new Dictionary<string, Dictionary<string, int[]>> {
            { "salsa20", new Dictionary<string, int[]> { { "salsa20", new int[] { 32, 8, CIPHER_SALSA20 } } } },
            { "chacha20", new Dictionary<string, int[]> { { "chacha20", new int[] { 32, 8, CIPHER_CHACHA20 } } } },
            { "chacha20-ietf", new Dictionary<string, int[]> { { "chacha20-ietf", new int[] { 32, 12, CIPHER_CHACHA20_IETF } } } }
        };

        protected override Dictionary<string, Dictionary<string, int[]>> getCiphers()
        {
            return _ciphers;
        }

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected override void cipherUpdate(bool isCipher, int length, byte[] buf, byte[] outbuf)
        {
            // TODO write a unidirection cipher so we don't have to if if if
            int bytesRemaining;
            ulong ic;
            byte[] iv;

            // I'm tired. just add a big lock
            // let's optimize for RAM instead of CPU
            lock(sodiumBuf)
            {
                if (isCipher)
                {
                    bytesRemaining = _encryptBytesRemaining;
                    ic = _encryptIC;
                    iv = _encryptIV;
                }
                else
                {
                    bytesRemaining = _decryptBytesRemaining;
                    ic = _decryptIC;
                    iv = _decryptIV;
                }
                int padding = bytesRemaining;
                Buffer.BlockCopy(buf, 0, sodiumBuf, padding, length);

                switch (_cipher)
                {
                    case CIPHER_SALSA20:
                        Sodium.crypto_stream_salsa20_xor_ic(sodiumBuf, sodiumBuf, (ulong)(padding + length), iv, ic, _key);
                        break;
                    case CIPHER_CHACHA20:
                        Sodium.crypto_stream_chacha20_xor_ic(sodiumBuf, sodiumBuf, (ulong)(padding + length), iv, ic, _key);
                        break;
                    case CIPHER_CHACHA20_IETF:
                        Sodium.crypto_stream_chacha20_ietf_xor_ic(sodiumBuf, sodiumBuf, (ulong)(padding + length), iv, (uint)ic, _key);
                        break;
                }
                Buffer.BlockCopy(sodiumBuf, padding, outbuf, 0, length);
                padding += length;
                ic += (ulong)padding / SODIUM_BLOCK_SIZE;
                bytesRemaining = padding % SODIUM_BLOCK_SIZE;

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
        }

        public override void Dispose()
        {
        }
    }
}
