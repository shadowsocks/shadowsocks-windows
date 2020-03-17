using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.AEAD
{
    public class AEADNativeEncryptor : AEADEncryptor
    {
        public AEADNativeEncryptor(string method, string password)
            : base(method, password)
        {
        }

        public override void cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            Array.Copy(ciphertext, plaintext, 0);
            plen = clen;

        }

        public override void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            Array.Copy(plaintext, ciphertext, 0);
            clen = plen;
        }

        public override void Dispose()
        {
            return;
        }

        private static Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>()
        {
            {"plain-fake-aead",new EncryptorInfo("PLAIN_AEAD",0,0,0,0,0) }
        };


        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        public static IEnumerable<string> SupportedCiphers()
        {
            return _ciphers.Keys;
        }

    }
}
