using System;
using System.Collections.Generic;
using System.Text;
using NaCl.Core;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamChachaNaClEncryptor : StreamEncryptor
    {
        ChaCha20 c;
        public StreamChachaNaClEncryptor(string method, string password) : base(method, password)
        {
            c = new ChaCha20(key, 0);
        }

        protected override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            var p = c.Decrypt(cipher, iv);
            p.CopyTo(plain);
            return p.Length;
        }

        protected override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            var e = c.Encrypt(plain, iv);
            e.CopyTo(cipher);
            return e.Length;
        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            var i = buf.AsSpan().Slice(0, length);
            if (isEncrypt)
            {
                CipherEncrypt(i, outbuf);
            }
            else
            {
                CipherDecrypt(outbuf, i);
            }
        }

        #region Ciphers
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            { "chacha20-ietf", new CipherInfo("chacha20-ietf", 32, 12, CipherFamily.Chacha20) },
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
