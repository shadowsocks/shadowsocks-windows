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

        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"aes-128-gcm", new CipherInfo("AES-128-GCM", 16, 16, 12, 16, CipherFamily.AesGcm)},
            {"aes-192-gcm", new CipherInfo("AES-192-GCM", 24, 24, 12, 16, CipherFamily.AesGcm)},
            {"aes-256-gcm", new CipherInfo("AES-256-GCM", 32, 32, 12, 16, CipherFamily.AesGcm)},
        };

        protected override Dictionary<string, CipherInfo> getCiphers()
        {
            return _ciphers;
        }

        public override void InitCipher(byte[] salt, bool isEncrypt, bool isUdp)
        {
            base.InitCipher(salt, isEncrypt, isUdp);

            DeriveSessionKey(isEncrypt ? encryptSalt : decryptSalt,
                 _Masterkey, sessionKey);
        }

        public override void cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            using (var aes = new AesGcm(sessionKey))
            {
                byte[] tag = new byte[tagLen];
                byte[] cipherWithoutTag = new byte[clen];
                Array.Copy(ciphertext, 0, cipherWithoutTag, 0, clen - tagLen);
                Array.Copy(ciphertext, clen - tagLen, tag, 0, tagLen);
                aes.Decrypt(decNonce, ciphertext, cipherWithoutTag, tag);
            }
            /*var cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(_sessionKey), tagLen * 8, _decNonce);

            cipher.Init(false, parameters);
            var plaintextBC = new byte[cipher.GetOutputSize((int)clen)];
            var len = cipher.ProcessBytes(ciphertext, 0, (int)clen, plaintextBC, 0);
            cipher.DoFinal(plaintextBC, len);
            plen = (uint)(plaintextBC.Length);
            Array.Copy(plaintextBC, 0, plaintext, 0, plen);*/
        }

        public override void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            using (var aes = new AesGcm(sessionKey))
            {
                byte[] tag = new byte[tagLen];
                byte[] cipherWithoutTag = new byte[clen];
                aes.Encrypt(encNonce, plaintext, cipherWithoutTag, tag);
                cipherWithoutTag.CopyTo(ciphertext, 0);
                tag.CopyTo(ciphertext, clen);
            }

            /*
            var cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(_sessionKey), tagLen * 8, _encNonce);

            cipher.Init(true, parameters);
            var ciphertextBC = new byte[cipher.GetOutputSize((int)plen)];
            var len = cipher.ProcessBytes(plaintext, 0, (int)plen, ciphertextBC, 0);
            cipher.DoFinal(ciphertextBC, len);
            clen = (uint)(ciphertextBC.Length);
            Array.Copy(ciphertextBC, 0, ciphertext, 0, clen);*/
        }

        public override byte[] CipherDecrypt2(byte[] cipher)
        {
            var (cipherMem, tagMem) = GetCipherTextAndTagMem(cipher);
            Span<byte> plainMem = new Span<byte>(new byte[cipherMem.Length]);

            using var aes = new AesGcm(sessionKey);
            aes.Decrypt(decNonce.AsSpan(), cipherMem.Span, tagMem.Span, plainMem);
            return plainMem.ToArray();
        }

        public override byte[] CipherEncrypt2(byte[] plain)
        {
            Span<byte> d = new Span<byte>(new byte[plain.Length + tagLen]);

            //Span<byte> cipherMem = new Span<byte>(new byte[plain.Length]);
            //Span<byte> tagMem = new Span<byte>(new byte[tagLen]);

            using var aes = new AesGcm(sessionKey);
            aes.Encrypt(encNonce.AsSpan(), plain.AsSpan(), d.Slice(0, plain.Length), d.Slice(plain.Length));

            return d.ToArray();
        }

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }
    }
}