using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Encrypt
{
    public class PolarSSL
    {
        const string DLLNAME = "libpolarssl";

        public const int AES_CTX_SIZE = 8 + 4 * 68;
        public const int AES_ENCRYPT = 1;
        public const int AES_DECRYPT = 0;

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void aes_init(byte[] ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void aes_free(byte[] ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int aes_setkey_enc(byte[] ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int aes_crypt_cfb128(byte[] ctx, int mode, int length, ref int iv_off, byte[] iv, byte[] input, byte[] output);


        public const int ARC4_CTX_SIZE = 264;

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_init(byte[] ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_free(byte[] ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void arc4_setup(byte[] ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int arc4_crypt(byte[] ctx, int length, byte[] input, byte[] output);


        public const int BLOWFISH_CTX_SIZE = 4168;
        public const int BLOWFISH_ENCRYPT = 1;
        public const int BLOWFISH_DECRYPT = 0;

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void blowfish_init(byte[] ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void blowfish_free(byte[] ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int blowfish_setkey(byte[] ctx, byte[] key, int keysize);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int blowfish_crypt_cfb64(byte[] ctx, int mode, int length, ref int iv_off, byte[] iv, byte[] input, byte[] output);

    }
}
