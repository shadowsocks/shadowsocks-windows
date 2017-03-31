using System;
using System.IO;
using System.Runtime.InteropServices;
using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    public static class Sodium
    {
        private const string DLLNAME = "libsscrypto.dll";

        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        static Sodium()
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

            lock (_initLock)
            {
                if (!_initialized)
                {
                    if (sodium_init() == -1)
                    {
                        throw new System.Exception("Failed to initialize sodium");
                    }
                    else /* 1 means already initialized; 0 means success */
                    {
                        _initialized = true;
                    }
                }
            }
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sodium_init();

        #region AEAD

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int sodium_increment(byte[] n, int nlen);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_chacha20poly1305_ietf_encrypt(byte[] c, ref ulong clen_p, byte[] m,
            ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_aead_chacha20poly1305_ietf_decrypt(byte[] m, ref ulong mlen_p,
            byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

        #endregion

        #region Stream

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic,
            byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic,
            byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int crypto_stream_chacha20_ietf_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, uint ic,
            byte[] k);

        #endregion
    }
}