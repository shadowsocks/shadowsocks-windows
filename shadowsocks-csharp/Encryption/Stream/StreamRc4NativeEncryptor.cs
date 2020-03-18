using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamRc4NativeEncryptor : StreamEncryptor
    {
        const int Rc4 = 0;
        const int Rc4Md5 = 1;

        string _password;

        byte[] realkey;
        byte[] sbox;
        public StreamRc4NativeEncryptor(string method, string password) : base(method, password)
        {
            _password = password;
        }

        public override void Dispose()
        {
            return;
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            base.initCipher(iv, isEncrypt);
            if (_cipher == Rc4Md5)
            {
                byte[] temp = new byte[keyLen + ivLen];
                Array.Copy(_key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                realkey = CryptoUtils.MD5(temp);
            }
            else
            {
                realkey = _key;
            }
            sbox = SBox(realkey);

        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            var ctx = isEncrypt ? enc_ctx : dec_ctx;

            byte[] t = new byte[length];
            Array.Copy(buf, t, length);

            RC4(ctx, sbox, t, length);
            Array.Copy(t, outbuf, length);
        }

        private static readonly Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>
        {
            // original RC4 doesn't use IV
            { "rc4", new EncryptorInfo("RC4", 16, 0, Rc4) },
            { "rc4-md5", new EncryptorInfo("RC4", 16, 16, Rc4Md5) },
        };

        public static IEnumerable<string> SupportedCiphers()
        {
            return _ciphers.Keys;
        }

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        #region RC4
        class Context
        {
            public int index1 = 0;
            public int index2 = 0;
        }

        private Context enc_ctx = new Context();
        private Context dec_ctx = new Context();

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

        private void RC4(Context ctx, byte[] s, byte[] data, int length)
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

        private static void Swap(byte[] s, int i, int j)
        {
            byte c = s[i];

            s[i] = s[j];
            s[j] = c;
        }
        #endregion
    }
}
