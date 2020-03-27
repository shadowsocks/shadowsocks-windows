using System;
using System.Collections.Generic;
using NaCl.Core;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamChachaNaClEncryptor : StreamEncryptor
    {
        // yes, they update over all saved data everytime...
        byte[] chachaBuf = new byte[65536];
        int ptr = 0;
        public StreamChachaNaClEncryptor(string method, string password) : base(method, password)
        {
        }

        protected override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            int len = cipher.Length;
            cipher.CopyTo(chachaBuf.AsSpan(ptr));
            var p = new ChaCha20(key, 0).Decrypt(chachaBuf, iv);
            p.AsSpan(ptr, len).CopyTo(plain);
            ptr += len;
            return len;
        }

        protected override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            int len = plain.Length;
            plain.CopyTo(chachaBuf.AsSpan(ptr));
            var p = new ChaCha20(key, 0).Encrypt(chachaBuf, iv);
            p.AsSpan(ptr, len).CopyTo(cipher);
            ptr += len;
            return len;
        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            var i = buf.AsSpan(0, length);
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
