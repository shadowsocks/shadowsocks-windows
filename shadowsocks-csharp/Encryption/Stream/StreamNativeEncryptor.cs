using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamNativeEncryptor : StreamEncryptor
    {
        const int Plain = 0;
        const int Table = 1;
        const int Rc4 = 2;
        const int Rc4Md5 = 3;

        string _password;

        byte[] realkey;
        byte[] sbox;
        public StreamNativeEncryptor(string method, string password) : base(method, password)
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
            if (_cipher >= Rc4)
            {
                if (_cipher == Rc4Md5)
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
                sbox = SBox(realkey);
            }
            else if (_cipher == Table)
            {
                ulong a = BitConverter.ToUInt64(MbedTLS.MD5(Encoding.UTF8.GetBytes(_password)), 0);
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
        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            if (_cipher == Table)
            {
                var table = isEncrypt ? _encryptTable : _decryptTable;
                for (int i = 0; i < length; i++)
                {
                    outbuf[i] = table[buf[i]];
                }
            }
            else if (_cipher == Plain)
            {
                Array.Copy(buf, outbuf, length);
            }
            else
            {
                var ctx = isEncrypt ? enc_ctx : dec_ctx;

                byte[] t = new byte[length];
                Array.Copy(buf, t, length);

                RC4(ctx, sbox, t, length);
                Array.Copy(t, outbuf, length);
            }
        }

        private static readonly Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>
        {
            {"plain", new EncryptorInfo("PLAIN", 0, 0, Plain) },
            {"table", new EncryptorInfo("TABLE", 0, 0, Table) },
            { "rc4", new EncryptorInfo("RC4", 16, 0, Rc4) }, // original RC4 doesn't use IV
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
