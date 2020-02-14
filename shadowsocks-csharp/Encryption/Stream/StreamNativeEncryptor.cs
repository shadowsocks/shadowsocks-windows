using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamNativeEncryptor : StreamEncryptor
    {
        bool md5;
        byte[] realkey;
        byte[] sbox;
        public StreamNativeEncryptor(string method, string password) : base(method, password)
        {
            md5 = method.ToLowerInvariant().IndexOf("md5") >= 0;
        }

        public override void Dispose()
        {
            return;
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            base.initCipher(iv, isEncrypt);
            if (md5)
            {
                byte[] temp = new byte[keyLen + ivLen];
                Array.Copy(_key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                realkey = MbedTLS.MD5(temp);
            }
            else
            {
                realkey = _key;
            }
            sbox = EncryptInitalize(realkey);
        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            var ctx = isEncrypt ? enc_ctx : dec_ctx;

            byte[] t = new byte[length];
            Array.Copy(buf, t, length);

            EncryptOutput(ctx, sbox, t, length);
            Array.Copy(t, outbuf, length);
        }

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return new Dictionary<string, EncryptorInfo>()
            {
                { "rc4-md5", new EncryptorInfo("rc4", 16, 16, 1) }
            };
        }

        class Context
        {
            public int index1 = 0;
            public int index2 = 0;
        }

        private Context enc_ctx = new Context();
        private Context dec_ctx = new Context();

        private byte[] EncryptInitalize(byte[] key)
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

        private void EncryptOutput(Context ctx, byte[] s, byte[] data, int length)
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
    }
}
