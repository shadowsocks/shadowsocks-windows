using System;
using System.Collections.Generic;
using System.Text;

namespace shadowsocks_csharp
{
    public class RC4
    {
        class Context
        {
            public int index1 = 0;
            public int index2 = 0;
        }

        private Context enc_ctx = new Context();
        private Context dec_ctx = new Context();

        public void Encrypt(byte[] table, byte[] data, int length)
        {
            EncryptOutput(enc_ctx, table, data, length);
        }

        public void Decrypt(byte[] table, byte[] data, int length)
        {
            EncryptOutput(dec_ctx, table, data, length);
        }

        public byte[] EncryptInitalize(byte[] key)
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
