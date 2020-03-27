using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamAesBouncyCastleEncryptor : StreamEncryptor
    {
        IBufferedCipher c;
        public StreamAesBouncyCastleEncryptor(string method, string password) : base(method, password)
        {
            c = new BufferedBlockCipher(new CfbBlockCipher(new AesEngine(), 128));
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            base.initCipher(iv, isEncrypt);
            c.Init(isEncrypt, new ParametersWithIV(new KeyParameter(key), iv));
        }

        protected override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            CipherUpdate(plain, cipher);
            return plain.Length;
        }

        protected override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            CipherUpdate(cipher, plain);
            return cipher.Length;
        }

        private void CipherUpdate(Span<byte> i, Span<byte> o)
        {
            // there's some secret in OpenSSL's EVP context.
            var ob = new byte[o.Length];
            int blklen = c.ProcessBytes(i.ToArray(), 0, i.Length, ob, 0);
            int restlen = i.Length - blklen;
            if (restlen != 0)
            {
                c.DoFinal(ob, blklen);
            }
            ob.CopyTo(o);
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"aes-128-cfb",new CipherInfo("aes-128-cfb", 16, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
            {"aes-192-cfb",new CipherInfo("aes-192-cfb", 24, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
            {"aes-256-cfb",new CipherInfo("aes-256-cfb", 32, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
        };

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }

        protected override Dictionary<string, CipherInfo> getCiphers()
        {
            return _ciphers;
        }
        #endregion
    }
}
