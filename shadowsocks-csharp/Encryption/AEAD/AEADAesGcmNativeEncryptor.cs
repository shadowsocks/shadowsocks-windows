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

        public override void cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            using (var aes = new AesGcm(sessionKey))
            {
                byte[] tag = new byte[tagLen];
                byte[] cipherWithoutTag = new byte[clen];
                Array.Copy(ciphertext, 0, cipherWithoutTag, 0, clen - tagLen);
                Array.Copy(ciphertext, clen - tagLen, tag, 0, tagLen);
                aes.Decrypt(nonce, ciphertext, cipherWithoutTag, tag);
            }
        }

        public override void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            using (var aes = new AesGcm(sessionKey))
            {
                byte[] tag = new byte[tagLen];
                byte[] cipherWithoutTag = new byte[clen];
                aes.Encrypt(nonce, plaintext, cipherWithoutTag, tag);
                cipherWithoutTag.CopyTo(ciphertext, 0);
                tag.CopyTo(ciphertext, clen);
            }
        }

        public override byte[] CipherDecrypt2(byte[] cipher)
        {
            var (cipherMem, tagMem) = GetCipherTextAndTagMem(cipher);
            Span<byte> plainMem = new Span<byte>(new byte[cipherMem.Length]);

            using var aes = new AesGcm(sessionKey);
            aes.Decrypt(nonce.AsSpan(), cipherMem.Span, tagMem.Span, plainMem);
            return plainMem.ToArray();
        }

        public override byte[] CipherEncrypt2(byte[] plain)
        {
            Span<byte> d = new Span<byte>(new byte[plain.Length + tagLen]);
            using var aes = new AesGcm(sessionKey);
            aes.Encrypt(nonce.AsSpan(), plain.AsSpan(), d.Slice(0, plain.Length), d.Slice(plain.Length));

            return d.ToArray();
        }
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
            aes.Decrypt(nonce.AsSpan(), plain, cipher.Slice(0, clen), cipher.Slice(clen));
            return clen;
        }
    }
}