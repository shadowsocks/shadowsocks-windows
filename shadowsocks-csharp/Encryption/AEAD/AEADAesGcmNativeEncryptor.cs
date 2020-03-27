using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Encryption.AEAD
{
    public class AEADAesGcmNativeEncryptor : AEADEncryptor
    {
        public AEADAesGcmNativeEncryptor(string method, string password) : base(method, password)
        {
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"aes-128-gcm", new CipherInfo("aes-128-gcm", 16, 16, 12, 16, CipherFamily.AesGcm)},
            {"aes-192-gcm", new CipherInfo("aes-192-gcm", 24, 24, 12, 16, CipherFamily.AesGcm)},
            {"aes-256-gcm", new CipherInfo("aes-256-gcm", 32, 32, 12, 16, CipherFamily.AesGcm)},
        };

        protected override Dictionary<string, CipherInfo> getCiphers()
        {
            return _ciphers;
        }

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }
        #endregion

        public override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            using var aes = new AesGcm(sessionKey);
            aes.Encrypt(nonce.AsSpan(), plain, cipher.Slice(0, plain.Length), cipher.Slice(plain.Length, tagLen));
            return plain.Length + tagLen;
        }

        public override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            int clen = cipher.Length - tagLen;
            using var aes = new AesGcm(sessionKey);
            var ciphertxt = cipher.Slice(0, clen);
            var tag = cipher.Slice(clen);
            aes.Decrypt(nonce.AsSpan(), ciphertxt, tag, plain.Slice(0, clen));
            return clen;
        }
    }
}