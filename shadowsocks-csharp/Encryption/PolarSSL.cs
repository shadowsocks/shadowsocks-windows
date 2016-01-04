using System;
using System.IO;
using System.Runtime.InteropServices;

using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    public class PolarSSL
    {
        const string DLLNAME = "libsscrypto";

        public const int AES_CTX_SIZE = 8 + 4 * 68;
        public const int AES_ENCRYPT = 1;
        public const int AES_DECRYPT = 0;

        static PolarSSL()
        {
            string dllPath = Utils.GetTempPath("libsscrypto.dll");
            try
            {
                FileManager.UncompressFile(dllPath, Resources.libsscrypto_dll);
            }
            catch (IOException)
            {
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
            LoadLibrary(dllPath);
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void aes_init(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void aes_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int aes_setkey_enc(IntPtr ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int aes_crypt_cfb128(IntPtr ctx, int mode, int length, ref int iv_off, byte[] iv, byte[] input, byte[] output);

        public const int ARC4_CTX_SIZE = 264;

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_init(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_setup(IntPtr ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int arc4_crypt(IntPtr ctx, int length, byte[] input, byte[] output);
    }
}
