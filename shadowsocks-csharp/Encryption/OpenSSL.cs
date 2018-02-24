using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Shadowsocks.Controller;
using Shadowsocks.Encryption.Exception;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    // XXX: only for OpenSSL 1.1.0 and higher
    public static class OpenSSL
    {
        private const string DLLNAME = "libsscrypto.dll";

        public const int OPENSSL_ENCRYPT = 1;
        public const int OPENSSL_DECRYPT = 0;

        public const int EVP_CTRL_AEAD_SET_IVLEN = 0x9;
        public const int EVP_CTRL_AEAD_GET_TAG = 0x10;
        public const int EVP_CTRL_AEAD_SET_TAG = 0x11;

        static OpenSSL()
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

        public static IntPtr GetCipherInfo(string cipherName)
        {
            var name = Encoding.ASCII.GetBytes(cipherName);
            Array.Resize(ref name, name.Length + 1);
            return EVP_get_cipherbyname(name);
        }

        /// <summary>
        /// Need init cipher context after EVP_CipherFinal_ex to reuse context
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="cipherType"></param>
        /// <param name="nonce"></param>
        public static void SetCtxNonce(IntPtr ctx, byte[] nonce, bool isEncrypt)
        {
            var ret = EVP_CipherInit_ex(ctx, IntPtr.Zero,
                IntPtr.Zero, null,
                nonce,
                isEncrypt ? OPENSSL_ENCRYPT : OPENSSL_DECRYPT);
            if (ret != 1) throw new System.Exception("openssl: fail to set AEAD nonce");
        }

        public static void AEADGetTag(IntPtr ctx, byte[] tagbuf, int taglen)
        {
            IntPtr tagBufIntPtr = IntPtr.Zero;
            try
            {
                tagBufIntPtr = Marshal.AllocHGlobal(taglen);
                var ret = EVP_CIPHER_CTX_ctrl(ctx,
                    EVP_CTRL_AEAD_GET_TAG, taglen, tagBufIntPtr);
                if (ret != 1) throw new CryptoErrorException("openssl: fail to get AEAD tag");
                // take tag from unmanaged memory
                Marshal.Copy(tagBufIntPtr, tagbuf, 0, taglen);
            }
            finally
            {
                if (tagBufIntPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tagBufIntPtr);
                }
            }
        }

        public static void AEADSetTag(IntPtr ctx, byte[] tagbuf, int taglen)
        {
            IntPtr tagBufIntPtr = IntPtr.Zero;
            try
            {
                // allocate unmanaged memory for tag
                tagBufIntPtr = Marshal.AllocHGlobal(taglen);

                // copy tag to unmanaged memory
                Marshal.Copy(tagbuf, 0, tagBufIntPtr, taglen);

                var ret = EVP_CIPHER_CTX_ctrl(ctx,
                    EVP_CTRL_AEAD_SET_TAG, taglen, tagBufIntPtr);

                if (ret != 1) throw new CryptoErrorException("openssl: fail to set AEAD tag");

            }
            finally
            {
                if (tagBufIntPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tagBufIntPtr);
                }
            }
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_CIPHER_CTX_new();

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EVP_CIPHER_CTX_free(IntPtr ctx);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CIPHER_CTX_reset(IntPtr ctx);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CipherInit_ex(IntPtr ctx, IntPtr type,
            IntPtr impl, byte[] key, byte[] iv, int enc);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CipherUpdate(IntPtr ctx, byte[] outb,
            out int outl, byte[] inb, int inl);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CipherFinal_ex(IntPtr ctx, byte[] outm, ref int outl);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CIPHER_CTX_set_padding(IntPtr x, int padding);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CIPHER_CTX_set_key_length(IntPtr x, int keylen);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CIPHER_CTX_ctrl(IntPtr ctx, int type, int arg, IntPtr ptr);

        /// <summary>
        /// simulate NUL-terminated string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_get_cipherbyname(byte[] name);
    }
}