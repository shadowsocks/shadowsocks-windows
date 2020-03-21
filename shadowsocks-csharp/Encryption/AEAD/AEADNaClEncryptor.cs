using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NaCl.Core;
using NaCl.Core.Base;

namespace Shadowsocks.Encryption.AEAD
{
    public class AEADNaClEncryptor : AEADEncryptor
    {

        SnufflePoly1305 enc;
        SnufflePoly1305 dec;
        public AEADNaClEncryptor(string method, string password) : base(method, password)
        {

        }

        public override void InitCipher(byte[] salt, bool isEncrypt, bool isUdp)
        {
            base.InitCipher(salt, isEncrypt, isUdp);
            DeriveSessionKey(isEncrypt ? encryptSalt : decryptSalt,
                 _Masterkey, sessionKey);

            SnufflePoly1305 tmp;
            switch (_cipher)
            {
                default:
                case CipherChaCha20Poly1305:
                    tmp = new ChaCha20Poly1305(sessionKey);
                    break;
                case CipherXChaCha20Poly1305:
                    tmp = new XChaCha20Poly1305(sessionKey);
                    break;
            }
            if (isEncrypt) enc = tmp;
            else dec = tmp;
        }

        public override void cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            var pt = dec.Decrypt(ciphertext, null, decNonce);
            pt.CopyTo(plaintext, 0);
            plen = (uint)pt.Length;
        }

        public override void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            var ct = enc.Encrypt(plaintext, null, encNonce);
            ct.CopyTo(ciphertext, 0);
            clen = (uint)ct.Length;
        }

        public override void Dispose()
        {
        }

        const int CipherChaCha20Poly1305 = 1;
        const int CipherXChaCha20Poly1305 = 2;

        private static readonly Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>
        {
            {"chacha20-ietf-poly1305", new EncryptorInfo(32, 32, 12, 16, 1)},
            {"xchacha20-ietf-poly1305", new EncryptorInfo(32, 32, 24, 16, 2)},
            //{"aes-256-gcm", new EncryptorInfo(32, 32, 12, 16, CIPHER_AES256GCM)},
        };

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        public override byte[] CipherEncrypt2(byte[] plain)
        {
            return  enc.Encrypt(plain, null, encNonce);
        }

        public override byte[] CipherDecrypt2(byte[] cipher)
        {
            return dec.Decrypt(cipher, null, decNonce);
        }
    }
}
