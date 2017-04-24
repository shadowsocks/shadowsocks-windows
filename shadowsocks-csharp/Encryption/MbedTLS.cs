using System;
using System.IO;
using System.Runtime.InteropServices;
using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    public static class MbedTLS
    {
        private const string DLLNAME = "libsscrypto.dll";

        public const int MBEDTLS_ENCRYPT = 1;
        public const int MBEDTLS_DECRYPT = 0;

        static MbedTLS()
        {
            string dllPath = Utils.GetTempPath(DLLNAME);
            try
            {
                FileManager.UncompressFile(dllPath, Resources.libsscrypto_dll);
            }
            catch (IOException)
            {
            }
            catch (System.Exception e)
            {
                Logging.LogUsefulException(e);
            }
            LoadLibrary(dllPath);
        }

        public static byte[] MD5(byte[] input)
        {
            byte[] output = new byte[16];
            md5(input, (uint) input.Length, output);
            return output;
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void md5(byte[] input, uint ilen, byte[] output);

        /// <summary>
        /// Get cipher ctx size for unmanaged memory allocation
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_get_size_ex();

        #region Cipher layer wrappers

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
        public static extern int cipher_auth_encrypt(IntPtr ctx,
            byte[] iv, uint iv_len,
            IntPtr ad, uint ad_len,
            byte[] input, uint ilen,
            byte[] output, ref uint olen,
            byte[] tag, uint tag_len);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int cipher_auth_decrypt(IntPtr ctx,
            byte[] iv, uint iv_len,
            IntPtr ad, uint ad_len,
            byte[] input, uint ilen,
            byte[] output, ref uint olen,
            byte[] tag, uint tag_len);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int hkdf(byte[] salt,
            int salt_len, byte[] ikm, int ikm_len,
            byte[] info, int info_len, byte[] okm,
            int okm_len);

        #endregion
    }
}