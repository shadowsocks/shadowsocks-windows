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
                case CipherFamily.Chacha20Poly1305:
                    tmp = new ChaCha20Poly1305(sessionKey);
                    break;
                case CipherFamily.XChacha20Poly1305:
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

        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"chacha20-ietf-poly1305", new CipherInfo("chacha20-ietf-poly1305",32, 32, 12, 16, CipherFamily.Chacha20Poly1305)},
            {"xchacha20-ietf-poly1305", new CipherInfo("xchacha20-ietf-poly1305",32, 32, 24, 16, CipherFamily.XChacha20Poly1305)},
        };

        protected override Dictionary<string, CipherInfo> getCiphers()
        {
            return _ciphers;
        }

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }

        public override byte[] CipherEncrypt2(byte[] plain)
        {
            return enc.Encrypt(plain, null, encNonce);
        }

        public override byte[] CipherDecrypt2(byte[] cipher)
        {
            return dec.Decrypt(cipher, null, decNonce);
        }
    }
}
