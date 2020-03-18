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
        static int CIPHER_AES = 1;  // dummy

        public AEADBouncyCastleEncryptor(string method, string password)
    : base(method, password)
        {
        }


        private static readonly Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>
        {
            {"aes-128-gcm", new EncryptorInfo("AES-128-GCM", 16, 16, 12, 16, CIPHER_AES)},
            {"aes-192-gcm", new EncryptorInfo("AES-192-GCM", 24, 24, 12, 16, CIPHER_AES)},
            {"aes-256-gcm", new EncryptorInfo("AES-256-GCM", 32, 32, 12, 16, CIPHER_AES)},
        };

        protected override Dictionary<string, EncryptorInfo> getCiphers()
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
            var cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, decNonce);

            cipher.Init(false, parameters);
            //var plaintextBC = new byte[cipher.GetOutputSize(ciphertext.Length)];
            var len = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
            cipher.DoFinal(plaintext, len);
            //plen = (uint)(plaintext.Length);
            //Array.Copy(plaintextBC, 0, plaintext, 0, plaintext.Length);
        }

        public override void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, encNonce);

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
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, decNonce);

            aes.Init(false, parameters);
            byte[] plain = new byte[aes.GetOutputSize(cipher.Length)];
            var len = aes.ProcessBytes(cipher, 0, cipher.Length, plain, 0);
            aes.DoFinal(plain, len);

            return plain;
        }

        public override byte[] CipherEncrypt2(byte[] plain)
        {
            var aes = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, encNonce);

            aes.Init(true, parameters);
            var cipher = new byte[aes.GetOutputSize(plain.Length)];

            var len = aes.ProcessBytes(plain, 0, plain.Length, cipher, 0);
            aes.DoFinal(cipher, len);
            return cipher;
        }
        
        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        public override void Dispose()
        {
        }
    }
}
