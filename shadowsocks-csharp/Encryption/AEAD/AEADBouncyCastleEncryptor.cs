using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Encryption.AEAD
{
    public class AEADBouncyCastleEncryptor : AEADEncryptor
    {
        public AEADBouncyCastleEncryptor(string method, string password) : base(method, password)
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
        public override void InitCipher(byte[] salt, bool isEncrypt, bool isUdp)
        {
            base.InitCipher(salt, isEncrypt, isUdp);

            DeriveSessionKey(salt, masterKey, sessionKey);
        }

        public override void cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, nonce);

            cipher.Init(false, parameters);
            var len = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
            cipher.DoFinal(plaintext, len);
        }

        public override void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, nonce);

            cipher.Init(true, parameters);
            var ciphertextBC = new byte[cipher.GetOutputSize((int)plen)];
            var len = cipher.ProcessBytes(plaintext, 0, (int)plen, ciphertextBC, 0);
            cipher.DoFinal(ciphertextBC, len);
            clen = (uint)(ciphertextBC.Length);
            Array.Copy(ciphertextBC, 0, ciphertext, 0, clen);
        }

        public override byte[] CipherDecrypt2(byte[] cipher)
        {
            var aes = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, nonce);

            aes.Init(false, parameters);
            byte[] plain = new byte[aes.GetOutputSize(cipher.Length)];
            var len = aes.ProcessBytes(cipher, 0, cipher.Length, plain, 0);
            aes.DoFinal(plain, len);

            return plain;
        }

        public override byte[] CipherEncrypt2(byte[] plain)
        {
            var aes = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, nonce);

            aes.Init(true, parameters);
            var cipher = new byte[aes.GetOutputSize(plain.Length)];

            var len = aes.ProcessBytes(plain, 0, plain.Length, cipher, 0);
            aes.DoFinal(cipher, len);
            return cipher;
        }

        public override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            throw new NotImplementedException();
        }

        public override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            throw new NotImplementedException();
        }
    }
}
