using System;

namespace shadowsocks_csharp.Encrypt
{
    public class TableEncryptor
        : EncryptorBase
    {
        public TableEncryptor(string method, string password)
            : base(method, password)
        {
            byte[] hash = GetPasswordHash();
            // TODO endian
            ulong a = BitConverter.ToUInt64(hash, 0);
            for (int i = 0; i < 256; i++)
            {
                _encryptTable[i] = (byte)i;
            }
            for (int i = 1; i < 1024; i++)
            {
                _encryptTable = MergeSort(_encryptTable, a, i);
            }
            for (int i = 0; i < 256; i++)
            {
                _decryptTable[_encryptTable[i]] = (byte)i;
            }
        }

        public override byte[] Encrypt(byte[] buf, int length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = _encryptTable[buf[i]];
            }
            return result;
        }

        public override byte[] Decrypt(byte[] buf, int length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = _decryptTable[buf[i]];
            }
            return result;
        }

        private readonly byte[] _encryptTable = new byte[256];
        private readonly byte[] _decryptTable = new byte[256];

        private static long Compare(byte x, byte y, ulong a, int i)
        {
            return (long)(a % (ulong)(x + i)) - (long)(a % (ulong)(y + i));
        }

        private byte[] MergeSort(byte[] array, ulong a, int j)
        {
            if (array.Length == 1)
            {
                return array;
            }
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
            left = MergeSort(left, a, j);
            right = MergeSort(right, a, j);

            int leftptr = 0;
            int rightptr = 0;

            byte[] sorted = new byte[array.Length];
            for (int k = 0; k < array.Length; k++)
            {
                if (rightptr == right.Length || ((leftptr < left.Length) && (Compare(left[leftptr], right[rightptr], a, j) <= 0)))
                {
                    sorted[k] = left[leftptr];
                    leftptr++;
                }
                else if (leftptr == left.Length || ((rightptr < right.Length) && (Compare(right[rightptr], left[leftptr], a, j)) <= 0))
                {
                    sorted[k] = right[rightptr];
                    rightptr++;
                }
            }
            return sorted;
        }
    }
}
