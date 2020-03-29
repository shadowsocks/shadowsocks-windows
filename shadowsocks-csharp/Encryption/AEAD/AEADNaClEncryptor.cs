using NaCl.Core;
using NaCl.Core.Base;
using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.AEAD
{
    public class AEADNaClEncryptor : AEADEncryptor
    {

        SnufflePoly1305? enc;
        public AEADNaClEncryptor(string method, string password) : base(method, password)
        {
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"chacha20-ietf-poly1305", new CipherInfo("chacha20-ietf-poly1305",32, 32, 12, 16, CipherFamily.Chacha20Poly1305)},
            {"xchacha20-ietf-poly1305", new CipherInfo("xchacha20-ietf-poly1305",32, 32, 24, 16, CipherFamily.XChacha20Poly1305)},
        };

        protected override Dictionary<string, CipherInfo> GetCiphers()
        {
            return _ciphers;
        }

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }
        #endregion

        public override void InitCipher(byte[] salt, bool isEncrypt)
        {
            base.InitCipher(salt, isEncrypt);
            enc = cipherFamily switch
            {
                CipherFamily.XChacha20Poly1305 => new XChaCha20Poly1305(sessionKey),
                _ => new ChaCha20Poly1305(sessionKey),
            };
        }

        public override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            byte[] ct = enc.Encrypt(plain, null, nonce);
            ct.CopyTo(cipher);
            return ct.Length;
        }

        public override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            byte[] pt = enc.Decrypt(cipher, null, nonce);
            pt.CopyTo(plain);
            return pt.Length;
        }
    }
}
