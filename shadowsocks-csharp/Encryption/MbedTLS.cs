using System;
using System.IO;
using System.Runtime.InteropServices;
using NLog;
using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    public static class MbedTLS
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

#if AMD64
        private const string DPDLLNAME = "libcrypto-3-x64.dll";
        private const string DLLNAME = "libsscrypto64.dll";
#else
        private const string DLLNAME = "libsscrypto.dll";
#endif

        public const int MBEDTLS_ENCRYPT = 1;
        public const int MBEDTLS_DECRYPT = 0;

        static MbedTLS()
        {
            string dllPath = Utils.GetTempPath(DLLNAME);
#if AMD64
            string dpDllPath = Utils.GetTempPath(DPDLLNAME);

#endif
            try
            {
#if AMD64
                FileManager.UncompressFile(dpDllPath, Resources.libcrypto_3_x64_dll);
                FileManager.UncompressFile(dllPath, Resources.libsscrypto64_dll);
#else
                FileManager.UncompressFile(dllPath, Resources.libsscrypto_dll);
#endif

            }
            catch (IOException)
            {
            }
            catch (System.Exception e)
            {
                logger.LogUsefulException(e);
            }
#if AMD64
            LoadLibrary(dpDllPath);

#endif
            LoadLibrary(dllPath);
        }

        public static byte[] MD5(byte[] input)
        {
            byte[] output = new byte[16];
            if (md5_ret(input, (uint)input.Length, output) != 0)
                throw new System.Exception("mbedtls: MD5 failure");
            return output;
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int md5_ret(byte[] input, uint ilen, byte[] output);

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