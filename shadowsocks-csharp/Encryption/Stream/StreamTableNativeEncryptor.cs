using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamTableNativeEncryptor : StreamEncryptor
    {
        // table mode use special way to generate key
        readonly string _password;

        public StreamTableNativeEncryptor(string method, string password) : base(method, password)
        {
            _password = password;
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            // another cipher is plain, needn't a table
            if (cipherFamily != CipherFamily.Table) return;
            ulong a = BitConverter.ToUInt64(CryptoUtils.MD5(Encoding.UTF8.GetBytes(_password)), 0);
            for (int i = 0; i < 256; i++)
            {
                _encryptTable[i] = (byte)i;
            }
            // copy array 1024 times? excuse me?
            for (int i = 1; i < 1024; i++)
            {
                _encryptTable = MergeSort(_encryptTable, a, i);
            }
            for (int i = 0; i < 256; i++)
            {
                _decryptTable[_encryptTable[i]] = (byte)i;
            }
        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            if (cipherFamily == CipherFamily.Table)
            {
                var table = isEncrypt ? _encryptTable : _decryptTable;
                for (int i = 0; i < length; i++)
                {
                    outbuf[i] = table[buf[i]];
                }
            }
            else if (cipherFamily == CipherFamily.Plain)
            {
                Array.Copy(buf, outbuf, length);
            }
        }

        protected override int CipherDecrypt(Span<byte> plain, Span<byte> cipher)
        {
            if (cipherFamily == CipherFamily.Plain)
            {
                cipher.CopyTo(plain);
                return cipher.Length;
            }

            for (int i = 0; i < cipher.Length; i++)
            {
                plain[i] = _decryptTable[cipher[i]];
            }
            return cipher.Length;
        }

        protected override int CipherEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            if (cipherFamily == CipherFamily.Plain)
            {
                plain.CopyTo(cipher);
                return plain.Length;
            }

            for (int i = 0; i < plain.Length; i++)
            {
                cipher[i] = _decryptTable[plain[i]];
            }
            return plain.Length;
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"plain", new CipherInfo("plain", 0, 0, CipherFamily.Plain) },
            {"table", new CipherInfo("table", 0, 0, CipherFamily.Table) },
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

        #region Table
        private byte[] _encryptTable = new byte[256];
        private byte[] _decryptTable = new byte[256];

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

            // why a new array?
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
        #endregion
    }
}
