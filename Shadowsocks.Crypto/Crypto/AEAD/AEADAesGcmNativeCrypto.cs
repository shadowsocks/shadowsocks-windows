using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Shadowsocks.Crypto.AEAD
{
    public class AEADAesGcmNativeCrypto : AEADCrypto
    {
        public AEADAesGcmNativeCrypto(string method, string password) : base(method, password)
        {
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"aes-128-gcm", new CipherInfo("aes-128-gcm", 16, 16, 12, 16, CipherFamily.AesGcm)},
            {"aes-192-gcm", new CipherInfo("aes-192-gcm", 24, 24, 12, 16, CipherFamily.AesGcm)},
            {"aes-256-gcm", new CipherInfo("aes-256-gcm", 32, 32, 12, 16, CipherFamily.AesGcm)},
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

        AesGcm aes;
        public override void InitCipher(byte[] salt, bool isEncrypt)
        {
            base.InitCipher(salt, isEncrypt);
            aes = new AesGcm(sessionKey);
        }

        public override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            aes.Encrypt(nonce, plain, cipher.Slice(0, plain.Length), cipher.Slice(plain.Length, tagLen));
            return plain.Length + tagLen;
        }

        public override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            int clen = cipher.Length - tagLen;
            ReadOnlySpan<byte> ciphertxt = cipher.Slice(0, clen);
            ReadOnlySpan<byte> tag = cipher.Slice(clen);
            aes.Decrypt(nonce, ciphertxt, tag, plain.Slice(0, clen));
            return clen;
        }
    }
}
