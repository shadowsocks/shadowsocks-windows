using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace shadowsocks_csharp
{
    class Encryptor
    {
        public const int TYPE_TABLE = 1;
        public const int TYPE_RC4 = 2;

        public byte[] encryptTable = new byte[256];
        public byte[] decryptTable = new byte[256];
        public int method = TYPE_TABLE;
        public RC4 rc4 = null;

        private long compare(byte x, byte y, ulong a, int i)
        {
            return (long)(a % (ulong)(x + i)) - (long)(a % (ulong)(y + i));
        }

        private byte[] mergeSort(byte[] array, ulong a, int j)
        {
            if (array.Length == 1)
                return array;
            int middle = array.Length / 2;
            byte[] left = new byte[middle];
            for (int i = 0; i < middle; i++)
            {
                left[i] = array[i];
            }
            byte[] right = new byte[array.Length - middle];
            for (int i = 0; i < array.Length - middle; i++)
            {
                right[i] = array[i + middle];
            }
            left = mergeSort(left, a, j);
            right = mergeSort(right, a, j);

            int leftptr = 0;
            int rightptr = 0;

            byte[] sorted = new byte[array.Length];
            for (int k = 0; k < array.Length; k++)
            {
                if (rightptr == right.Length || ((leftptr < left.Length) && (compare(left[leftptr], right[rightptr], a, j) <= 0)))
                {
                    sorted[k] = left[leftptr];
                    leftptr++;
                }
                else if (leftptr == left.Length || ((rightptr < right.Length) && (compare(right[rightptr], left[leftptr], a, j)) <= 0))
                {
                    sorted[k] = right[rightptr];
                    rightptr++;
                }
            }
            return sorted;
        }

        public Encryptor(string method, string password)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] hash = md5.ComputeHash(inputBytes);

            if (method != null && method.ToLowerInvariant().Equals("rc4")) {
                Console.WriteLine("init rc4");
                this.method = TYPE_RC4;

                rc4 = new RC4();
                encryptTable = rc4.EncryptInitalize(hash);
                decryptTable = rc4.EncryptInitalize(hash);
            } else {
                Console.WriteLine("init table");
                this.method = TYPE_TABLE;

                // TODO endian
                var a = BitConverter.ToUInt64(hash, 0);
                for (int i = 0; i < 256; i++)
                {
                    encryptTable[i] = (byte)i;
                }
                for (int i = 1; i < 1024; i++)
                {
                    encryptTable = mergeSort(encryptTable, a, i);
                }
                for (int i = 0; i < 256; i++)
                {
                    decryptTable[encryptTable[i]] = (byte)i;
                }
            }
        }

        public void Encrypt(byte[] buf, int length)
        {
            switch (method)
            {
                case TYPE_TABLE:
                    for (int i = 0; i < length; i++)
                        buf[i] = encryptTable[buf[i]];
                    break;
                case TYPE_RC4:
                    rc4.Encrypt(encryptTable, buf, length);
                    break;
            }
        }
        public void Decrypt(byte[] buf, int length)
        {
            switch (method)
            {
                case TYPE_TABLE:
                    for (int i = 0; i < length; i++)
                        buf[i] = decryptTable[buf[i]];
                    break;
                case TYPE_RC4:
                    rc4.Decrypt(decryptTable, buf, length);
                    break;
            }
        }
    }
}
