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
            // c = CipherUtilities.GetCipher("AES/CFB/NoPadding");
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            base.initCipher(iv, isEncrypt);
            c.Init(isEncrypt, new ParametersWithIV(new KeyParameter(key), iv));
        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            var i = buf.AsSpan().Slice(0, length);
            if (isEncrypt) CipherEncrypt(i, outbuf);
            else CipherDecrypt(outbuf, i);
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
            var ob = new byte[o.Length];
            int blklen = c.ProcessBytes(i.ToArray(), 0, i.Length, ob, 0);
            int restlen = i.Length - blklen;
            if (restlen != 0)
            {
                c.DoFinal(ob, blklen);
            }
            ob.CopyTo(o);
        }

        #region Ciphers
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
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
