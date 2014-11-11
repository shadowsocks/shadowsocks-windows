using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Encrypt
{
    public class OpenSSL
    {
        const string DLLNAME = "libeay32";

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void OpenSSL_add_all_ciphers();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr EVP_md5();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int EVP_BytesToKey(IntPtr type, IntPtr md, IntPtr salt, byte[] data, int datal, int count, byte[] key, byte[] iv);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int RAND_bytes(byte[] buf, int num);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr EVP_get_cipherbyname(byte[] name);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr EVP_CIPHER_CTX_new();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int EVP_CipherInit_ex(IntPtr ctx, IntPtr type, IntPtr impl, byte[] key, byte[] iv, int enc);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int EVP_CIPHER_CTX_cleanup(IntPtr a);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int EVP_CIPHER_CTX_free(IntPtr a);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int EVP_CipherUpdate(IntPtr ctx, byte[] outb, out int outl, byte[] inb, int inl);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr MD5(byte[] d, long n, byte[] md);
    }
}
