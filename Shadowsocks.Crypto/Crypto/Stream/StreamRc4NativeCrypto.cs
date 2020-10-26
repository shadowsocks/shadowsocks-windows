using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shadowsocks.Crypto.Stream
{
    public class StreamRc4NativeCrypto : StreamCrypto
    {
        byte[] realkey = new byte[256];
        byte[] sbox = new byte[256];
        public StreamRc4NativeCrypto(string method, string password) : base(method, password)
        {
        }

        protected override void InitCipher(byte[] iv, bool isEncrypt)
        {
            base.InitCipher(iv, isEncrypt);
            // rc4-md5 is rc4 with md5 based session key
            if (cipherFamily == CipherFamily.Rc4Md5)
            {
                byte[] temp = new byte[keyLen + ivLen];
                Array.Copy(key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                realkey = CryptoUtils.MD5(temp);
            }
            else
            {
                realkey = key;
            }
            sbox = SBox(realkey);
        }

        protected override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            return CipherUpdate(plain, cipher);
        }

        protected override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            return CipherUpdate(cipher, plain);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CipherUpdate(ReadOnlySpan<byte> i, Span<byte> o)
        {
            // don't know why we need third array, but it works...
            Span<byte> t = new byte[i.Length];
            i.CopyTo(t);
            RC4(sbox, t, t.Length);
            t.CopyTo(o);
            return t.Length;
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            // original RC4 doesn't use IV
            { "rc4", new CipherInfo("rc4", 16, 0, CipherFamily.Rc4) },
            { "rc4-md5", new CipherInfo("rc4-md5", 16, 16, CipherFamily.Rc4Md5) },
        };

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }

        protected override Dictionary<string, CipherInfo> GetCiphers()
        {
            return _ciphers;
        }
        #endregion

        #region RC4

        int index1 = 0;
        int index2 = 0;

        private byte[] SBox(byte[] key)
        {
            byte[] s = new byte[256];

            for (int i = 0; i < 256; i++)
            {
                s[i] = (byte)i;
            }

            for (int i = 0, j = 0; i < 256; i++)
            {
                j = (j + key[i % key.Length] + s[i]) & 255;

                Swap(s, i, j);
            }

            return s;
        }

        private void RC4(Span<byte> s, Span<byte> data, int length)
        {
            for (int n = 0; n < length; n++)
            {
                byte b = data[n];

                index1 = (index1 + 1) & 255;
                index2 = (index2 + s[index1]) & 255;

                Swap(s, index1, index2);
                data[n] = (byte)(b ^ s[(s[index1] + s[index2]) & 255]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(Span<byte> s, int i, int j)
        {
            byte c = s[i];

            s[i] = s[j];
            s[j] = c;
        }
        #endregion
    }
}
