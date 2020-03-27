using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamRc4NativeEncryptor : StreamEncryptor
    {
        byte[] realkey = new byte[256];
        byte[] sbox = new byte[256];
        public StreamRc4NativeEncryptor(string method, string password) : base(method, password)
        {
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            base.initCipher(iv, isEncrypt);
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

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            byte[] t = new byte[length];
            Array.Copy(buf, t, length);

            RC4(ctx, sbox, t, length);
            Array.Copy(t, outbuf, length);
        }

        protected override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            return DoRC4(plain, cipher);
        }

        protected override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            return DoRC4(cipher, plain);
        }

        private int DoRC4(Span<byte> i, Span<byte> o)
        {
            i.CopyTo(o);
            RC4(ctx, sbox, o, o.Length);
            return o.Length;
        }

        #region Ciphers
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

        protected override Dictionary<string, CipherInfo> getCiphers()
        {
            return _ciphers;
        }
        #endregion

        #region RC4
        class Context
        {
            public int index1 = 0;
            public int index2 = 0;
        }

        private Context ctx = new Context();

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

        private void RC4(Context ctx, Span<byte> s, Span<byte> data, int length)
        {
            for (int n = 0; n < length; n++)
            {
                byte b = data[n];

                ctx.index1 = (ctx.index1 + 1) & 255;
                ctx.index2 = (ctx.index2 + s[ctx.index1]) & 255;

                Swap(s, ctx.index1, ctx.index2);
                data[n] = (byte)(b ^ s[(s[ctx.index1] + s[ctx.index2]) & 255]);
            }
        }

        private static void Swap(Span<byte> s, int i, int j)
        {
            byte c = s[i];

            s[i] = s[j];
            s[j] = c;
        }
        #endregion
    }
}
