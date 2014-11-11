using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Encrypt
{
    public class PolarSSL
    {
        const string DLLNAME = "polarssl";

        public const int AES_CTX_SIZE = 8 + 4 * 68;
        public const int AES_ENCRYPT = 1;
        public const int AES_DECRYPT = 0;

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void aes_init(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void aes_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int aes_setkey_enc(IntPtr ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int aes_crypt_cfb128(IntPtr ctx, int mode, int length, byte[] iv_off, byte[] iv, byte[] input, byte[] output);


        public const int ARC4_CTX_SIZE = 264;

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_init(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_setup(IntPtr ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int arc4_crypt(IntPtr ctx, int length, byte[] input, byte[] output);


        public const int BLOWFISH_CTX_SIZE = 4168;
        public const int BLOWFISH_ENCRYPT = 1;
        public const int BLOWFISH_DECRYPT = 0;

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void blowfish_init(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void blowfish_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int blowfish_setkey(IntPtr ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int blowfish_crypt_cfb64(IntPtr ctx, int mode, int length, byte[] iv_off, byte[] iv, byte[] input, byte[] output);

    }
}
