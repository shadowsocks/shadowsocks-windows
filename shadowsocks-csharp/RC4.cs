using System;
using System.Collections.Generic;
using System.Text;

namespace shadowsocks_csharp
{
    public class RC4
    {

        int enc_index1 = 0;
        int enc_index2 = 0;

        int dec_index1 = 0;
        int dec_index2 = 0;

        public void Encrypt(byte[] table, byte[] data, int length)
        {
            EncryptOutput(enc_index1, enc_index2, table, data, length);
        }

        public void Decrypt(byte[] table, byte[] data, int length)
        {
            EncryptOutput(dec_index1, dec_index2, table, data, length);
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

        private void EncryptOutput(int index1, int index2, byte[] s, byte[] data, int length)
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

        private static void Swap(byte[] s, int i, int j)
        {
            byte c = s[i];

            s[i] = s[j];
            s[j] = c;
        }
    }
}
