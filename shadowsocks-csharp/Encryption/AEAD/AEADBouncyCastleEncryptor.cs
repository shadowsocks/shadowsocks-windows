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
    public class AEADBouncyCastleEncryptor : AEADEncryptor//, IDisposable
    {
        public AEADBouncyCastleEncryptor(string method, string password)
    : base(method, password)
        {
        }

        static int CIPHER_AES=1;
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

        public override void cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(_sessionKey), tagLen * 8, _decNonce);

            cipher.Init(false, parameters);
            plaintext = new byte[cipher.GetOutputSize((int)clen)];
            var len = cipher.ProcessBytes(ciphertext, 0, (int)clen, plaintext, 0);
            cipher.DoFinal(plaintext, len);
            plen = (uint)(plaintext.Length);
        }

        public override void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(_sessionKey), tagLen * 8, _encNonce);

            cipher.Init(true, parameters);
            ciphertext = new byte[cipher.GetOutputSize((int)plen)];
            var len = cipher.ProcessBytes(plaintext, 0, (int)plen, ciphertext, 0);
            cipher.DoFinal(ciphertext, len);
            clen = (uint)(ciphertext.Length);
        }

        public override void Dispose()
        {
            //throw new NotImplementedException();
        }

        public override void InitCipher(byte[] salt, bool isEncrypt, bool isUdp)
        {
            base.InitCipher(salt, isEncrypt, isUdp);

            DeriveSessionKey(isEncrypt ? _encryptSalt : _decryptSalt,
                 _Masterkey, _sessionKey);
        }

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }
    }
}
