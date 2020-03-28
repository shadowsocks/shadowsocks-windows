using System;
using System.Collections.Generic;
using NaCl.Core;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamChachaNaClEncryptor : StreamEncryptor
    {
        const int BlockSize = 64;
        // tcp is stream, which can split into chunks at unexpected position...
        // so we need some special handling, as we can't read all data before encrypt
        
        // we did it in AEADEncryptor.cs for AEAD, it can operate at block level
        // but we need do it ourselves in stream cipher.

        // when new data arrive, put it on correct offset
        // and update it, ignore other data, get it in correct offset...
        byte[] chachaBuf = new byte[MaxInputSize + BlockSize];
        // the 'correct offset', always in 0~BlockSize range, so input data always fit into buffer
        int remain = 0;
        // increase counter manually...
        int ic = 0;
        public StreamChachaNaClEncryptor(string method, string password) : base(method, password)
        {
        }

        protected override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            return CipherUpdate(cipher, plain, false);
        }

        protected override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            return CipherUpdate(plain, cipher, true);
        }

        private int CipherUpdate(ReadOnlySpan<byte> i, Span<byte> o, bool enc)
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

        #region Cipher Info
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
