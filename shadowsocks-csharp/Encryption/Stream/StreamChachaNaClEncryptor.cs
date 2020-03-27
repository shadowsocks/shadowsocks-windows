using System;
using System.Collections.Generic;
using NaCl.Core;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamChachaNaClEncryptor : StreamEncryptor
    {
        const int BlockSize = 64;
        // when new data arrive, put it in correct offset of chunk
        // and update it, ignore other data, get it in correct offset...
        byte[] chachaBuf = new byte[32768 + BlockSize];
        int remain = 0;
        // increase counter only when a chunk fully recieved
        int ic = 0;
        public StreamChachaNaClEncryptor(string method, string password) : base(method, password)
        {
        }

        protected override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            return CipherUpdate(cipher, plain, false);
        }

        protected override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            return CipherUpdate(plain, cipher, true);
        }

        private int CipherUpdate(Span<byte> i, Span<byte> o, bool enc)
        {
            int len = i.Length;
            int pad = remain;
            i.CopyTo(chachaBuf.AsSpan(pad));
            var chacha = new ChaCha20(key, ic);
            var p = enc ? chacha.Encrypt(chachaBuf, iv) : chacha.Decrypt(chachaBuf, iv);
            p.AsSpan(pad, len).CopyTo(o);
            pad += len;
            ic += pad / BlockSize;
            remain = pad % BlockSize;
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
