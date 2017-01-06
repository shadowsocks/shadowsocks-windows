using System;
using System.IO;
using System.Runtime.InteropServices;

using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    public class MbedTLS
    {
        const string DLLNAME = "libsscrypto";

        public const int MBEDTLS_ENCRYPT = 1;
        public const int MBEDTLS_DECRYPT = 0;
        public const int MD5_CTX_SIZE = 88;

        public const int MBEDTLS_MD_MD5 = 3;
        public const int MBEDTLS_MD_SHA1 = 4;
        public const int MBEDTLS_MD_SHA224 = 5;
        public const int MBEDTLS_MD_SHA256 = 6;
        public const int MBEDTLS_MD_SHA384 = 7;
        public const int MBEDTLS_MD_SHA512 = 8;
        public const int MBEDTLS_MD_RIPEMD160 = 9;

        public interface HMAC
        {
            byte[] ComputeHash(byte[] buffer, int offset, int count);
        }

        public class HMAC_MD5 : HMAC
        {
            byte[] key;

            public HMAC_MD5(byte[] key)
            {
                this.key = key;
            }

            public byte[] ComputeHash(byte[] buffer, int offset, int count)
            {
                byte[] output = new byte[64];
                ss_hmac_ex(MBEDTLS_MD_MD5, key, key.Length, buffer, offset, count, output);
                return output;
            }
        }

        public class HMAC_SHA1 : HMAC
        {
            byte[] key;

            public HMAC_SHA1(byte[] key)
            {
                this.key = key;
            }

            public byte[] ComputeHash(byte[] buffer, int offset, int count)
            {
                byte[] output = new byte[64];
                ss_hmac_ex(MBEDTLS_MD_SHA1, key, key.Length, buffer,offset, count, output);
                return output;
            }
        }

        static MbedTLS()
        {
            string runningPath = Path.Combine(System.Windows.Forms.Application.StartupPath, @"temp"); // Path.GetTempPath();
            if (!Directory.Exists(runningPath))
            {
                Directory.CreateDirectory(runningPath);
            }
            string dllPath = Path.Combine(runningPath, "libsscrypto.dll");
            try
            {
                if (IntPtr.Size == 4)
                {
                    FileManager.UncompressFile(dllPath, Resources.libsscrypto_dll);
                }
                else
                {
                    FileManager.UncompressFile(dllPath, Resources.libsscrypto64_dll);
                }
            }
            catch (IOException)
            {
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
            IntPtr module = LoadLibrary(dllPath);
        }

        public static byte[] MD5(byte[] input)
        {
            byte[] output = new byte[16];
            md5(input, input.Length, output);
            return output;
        }

        public static byte[] SHA1(byte[] input)
        {
            byte[] output = new byte[20];
            ss_md(MBEDTLS_MD_SHA1, input, 0, input.Length, output);
            return output;
        }

        public static byte[] SHA512(byte[] input)
        {
            byte[] output = new byte[64];
            ss_md(MBEDTLS_MD_SHA512, input, 0, input.Length, output);
            return output;
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cipher_info_from_string(string cipher_name);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void cipher_init(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_setup(IntPtr ctx, IntPtr cipher_info);

        // XXX: Check operation before using it
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_setkey(IntPtr ctx, byte[] key, int key_bitlen, int operation);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_set_iv(IntPtr ctx, byte[] iv, int iv_len);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_reset(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_update(IntPtr ctx, byte[] input, int ilen, byte[] output, ref int olen);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void cipher_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void md5(byte[] input, int ilen, byte[] output);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ss_md(int md_type, byte[] input, int offset, int ilen, byte[] output);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ss_hmac_ex(int md_type, byte[] key, int keylen, byte[] input, int offset, int ilen, byte[] output);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_get_size_ex();

    }
}
