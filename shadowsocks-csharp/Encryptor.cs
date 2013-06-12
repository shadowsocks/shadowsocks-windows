using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using OpenSSL.Core;
using OpenSSL.Crypto;
using System.Runtime.InteropServices;

namespace shadowsocks_csharp
{
    class Encryptor : IDisposable
    {
        public static string[] encryption_names = new string[] {
            "table",
            "rc4",
            "aes-256-cfb",
            "aes-192-cfb",
            "aes-128-cfb",
            "bf-cfb"
        };

        public byte[] encryptTable = new byte[256];
        public byte[] decryptTable = new byte[256];
        public string method = "table";
        public string password;
        public byte[] key;
        private RC4 rc4 = null;
        private Cipher cipher = null;
        private IntPtr encryptCTX;
        private IntPtr decryptCTX;
        private static Dictionary<string, byte[]> cachedKeys = new Dictionary<string, byte[]>();
        private static Dictionary<string, Cipher> cachedCiphers = new Dictionary<string, Cipher>();

        public void Dispose()
        {
            if (encryptCTX != IntPtr.Zero)
            {
                Native.EVP_CIPHER_CTX_cleanup(encryptCTX);
                Native.OPENSSL_free(encryptCTX);
                encryptCTX = IntPtr.Zero;
            }
            if (decryptCTX != IntPtr.Zero)
            {
                Native.EVP_CIPHER_CTX_cleanup(decryptCTX);
                Native.OPENSSL_free(decryptCTX);
                decryptCTX = IntPtr.Zero;
            }
        }

        ~Encryptor() {
            Dispose();
        }

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

            encryptCTX = IntPtr.Zero;
            decryptCTX = IntPtr.Zero;

            this.method = method;
            this.password = password;
            if (method != null && method.ToLowerInvariant().Equals("rc4")) {
                Console.WriteLine("init rc4");

                rc4 = new RC4();
                encryptTable = rc4.EncryptInitalize(hash);
                decryptTable = rc4.EncryptInitalize(hash);
            }
            else if (method == "table" || method == "" || method == null)
            {
                Console.WriteLine("init table");

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
            else
            {
                initKey(password, method);
            }
        }

        private void initCipher(ref IntPtr ctx, byte[] iv, bool isCipher)
        {
            ctx = Native.OPENSSL_malloc(Marshal.SizeOf(typeof(CipherContext.EVP_CIPHER_CTX)));
            int enc = isCipher ? 1 : 0;
            Native.EVP_CIPHER_CTX_init(ctx);
            Native.ExpectSuccess(Native.EVP_CipherInit_ex(
                ctx, this.cipher.Handle, IntPtr.Zero, null, null, enc));
            Native.ExpectSuccess(Native.EVP_CIPHER_CTX_set_key_length(ctx, key.Length));
            Native.ExpectSuccess(Native.EVP_CIPHER_CTX_set_padding(ctx, 1));
            Native.ExpectSuccess(Native.EVP_CipherInit_ex(
                ctx, this.cipher.Handle, IntPtr.Zero, key, iv, enc));
        }

        private void initKey(string password, string method)
        {
            string k = method + ":" + password;
            if (cachedKeys.ContainsKey(k))
            {
                key = cachedKeys[k];
                cipher = cachedCiphers[k];
                return;
            }
            cipher = Cipher.CreateByName(method);
            if (cipher == null)
            {
                throw new NullReferenceException();
            }
            byte[] passbuf = System.Text.Encoding.UTF8.GetBytes(password); ;
            key =  new byte[cipher.KeyLength];
            byte[] iv = new byte[cipher.IVLength];
            Native.EVP_BytesToKey(cipher.Handle, MessageDigest.MD5.Handle, null, passbuf, passbuf.Length, 1, key, iv);
            cachedKeys[k] = key;
            cachedCiphers[k] = cipher;
        }

        private byte[] sslEncrypt(byte[] buf, int length)
        {
            if (encryptCTX == IntPtr.Zero)
            {
                int ivLen = cipher.IVLength;
                byte[] iv = new byte[ivLen];
                Native.RAND_bytes(iv, iv.Length);
                initCipher(ref encryptCTX, iv, true);
                int outLen = length + cipher.BlockSize;
                byte[] cipherText = new byte[outLen];
                Native.EVP_CipherUpdate(encryptCTX, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen + ivLen];
                System.Buffer.BlockCopy(iv, 0, result, 0, ivLen);
                System.Buffer.BlockCopy(cipherText, 0, result, ivLen, outLen);
                return result;
            }
            else
            {
                int outLen = length + cipher.BlockSize;
                byte[] cipherText = new byte[outLen];
                Native.EVP_CipherUpdate(encryptCTX, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen];
                System.Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
        }

        private byte[] sslDecrypt(byte[] buf, int length)
        {
            if (decryptCTX == IntPtr.Zero)
            {
                int ivLen = cipher.IVLength;
                byte[] iv = new byte[ivLen];
                System.Buffer.BlockCopy(buf, 0, iv, 0, ivLen);
                initCipher(ref decryptCTX, iv, false);
                int outLen = length + cipher.BlockSize;
                outLen -= ivLen;
                byte[] cipherText = new byte[outLen];
                byte[] subset = new byte[length - ivLen];
                System.Buffer.BlockCopy(buf, ivLen, subset, 0, length - ivLen);
                Native.EVP_CipherUpdate(decryptCTX, cipherText, out outLen, subset, length - ivLen);
                byte[] result = new byte[outLen];
                System.Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
            else
            {
                int outLen = length + cipher.BlockSize;
                byte[] cipherText = new byte[outLen];
                Native.EVP_CipherUpdate(decryptCTX, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen];
                System.Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
        }

        public byte[] Encrypt(byte[] buf, int length)
        {
            switch (method)
            {
                case "table":
                    for (int i = 0; i < length; i++)
                        buf[i] = encryptTable[buf[i]];
                    return buf;
                    break;
                case "rc4":
                    rc4.Encrypt(encryptTable, buf, length);
                    return buf;
                    break;
                default:
                    return sslEncrypt(buf, length);
            }
        }
        public byte[] Decrypt(byte[] buf, int length)
        {
            switch (method)
            {
                case "table":
                    for (int i = 0; i < length; i++)
                        buf[i] = decryptTable[buf[i]];
                    return buf;
                    break;
                case "rc4":
                    rc4.Decrypt(decryptTable, buf, length);
                    return buf;
                    break;
                default:
                    return sslDecrypt(buf, length);
            }
        }
    }
}
