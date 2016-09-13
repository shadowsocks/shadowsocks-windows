using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Shadowsocks.Controller;
using Shadowsocks.Properties;

namespace Shadowsocks.Encryption
{
    class Libcrypto
    {
        delegate IntPtr EncryptFunc();
        const string DLLNAME = "libeay32";
        static Dictionary<string, EncryptFunc> encrypt_func_map;

        static Libcrypto()
        {
            try
            {
                //try
                //{
                //    dlopen("libcrypto.so", 2);
                //    return;
                //}
                //catch (Exception e)
                //{
                //    //Console.WriteLine(e.ToString());
                //}
                string runningPath = Path.Combine(System.Windows.Forms.Application.StartupPath, @"temp"); // Path.GetTempPath();
                if (!Directory.Exists(runningPath))
                {
                    Directory.CreateDirectory(runningPath);
                }
                string dllPath = runningPath + "/libeay32.dll";
                try
                {
                    //FileManager.UncompressFile(dllPath, Resources.libsscrypto_dll);
                    LoadLibrary(dllPath);
                }
                catch (IOException)
                {
                }
                catch //(Exception e)
                {
                    //Console.WriteLine(e.ToString());
                }
            }
            finally
            {
                if (encrypt_func_map == null && isSupport())
                {
                    Dictionary<string, EncryptFunc> func_map = new Dictionary<string, EncryptFunc>();
                    func_map["rc4"] = EVP_rc4;
                    func_map["aes-128-cfb"] = EVP_aes_128_cfb;
                    func_map["aes-192-cfb"] = EVP_aes_192_cfb;
                    func_map["aes-256-cfb"] = EVP_aes_256_cfb;
                    func_map["bf-cfb"] = EVP_bf_cfb;
                    encrypt_func_map = func_map;
                    OpenSSL_add_all_ciphers();
                }
            }
        }

        public static bool isSupport()
        {
            try
            {
                IntPtr cipher = EVP_get_cipherbyname(null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool is_cipher(string cipher_name)
        {
            string real_cipher_name = cipher_name;
            if (cipher_name.StartsWith("rc4-md5"))
            {
                real_cipher_name = "rc4";
            }
            IntPtr ctx = IntPtr.Zero;
            byte[] cipher_name_buf = Encoding.ASCII.GetBytes(real_cipher_name);
            Array.Resize(ref cipher_name_buf, cipher_name_buf.Length + 1);
            IntPtr cipher = EVP_get_cipherbyname(cipher_name_buf);
            return cipher != IntPtr.Zero;
        }

        public static IntPtr init(string cipher_name, byte[] key, byte[] iv, int op)
        {
            IntPtr ctx = IntPtr.Zero;
            string real_cipher_name = cipher_name;
            if (cipher_name.StartsWith("rc4-md5"))
            {
                real_cipher_name = "rc4";
            }
            byte[] cipher_name_buf = Encoding.ASCII.GetBytes(real_cipher_name);
            Array.Resize(ref cipher_name_buf, cipher_name_buf.Length + 1);
            IntPtr cipher = EVP_get_cipherbyname(cipher_name_buf);
            if (cipher == IntPtr.Zero)
            {
                if (encrypt_func_map != null && encrypt_func_map.ContainsKey(real_cipher_name))
                {
                    cipher = encrypt_func_map[real_cipher_name]();
                }
            }
            if (cipher != IntPtr.Zero)
            {
                ctx = EVP_CIPHER_CTX_new();
                int r = EVP_CipherInit_ex(ctx, cipher, IntPtr.Zero, key, iv, op);
                if (r == 0)
                {
                    clean(ctx);
                    return IntPtr.Zero;
                }
            }
            return ctx;
        }

        public static int update(IntPtr ctx, byte[] data, int length, byte[] outbuf)
        {
            int out_len = 0;
            EVP_CipherUpdate(ctx, outbuf, ref out_len, data, length);
            return out_len;
        }

        public static void clean(IntPtr ctx)
        {
            EVP_CIPHER_CTX_cleanup(ctx);
            EVP_CIPHER_CTX_free(ctx);
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        //[DllImport("libdl.so")]
        //private static extern IntPtr dlopen(String fileName, int flags);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void OpenSSL_add_all_ciphers();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_add_cipher(byte[] cipher_name);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_get_cipherbyname(byte[] cipher_name);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_aes_256_cfb();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_aes_192_cfb();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_aes_128_cfb();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_rc4();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_bf_cfb();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EVP_CIPHER_CTX_new();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EVP_CIPHER_CTX_cleanup(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EVP_CIPHER_CTX_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EVP_CipherInit_ex(IntPtr ctx, IntPtr cipher, IntPtr _, byte[] key, byte[] iv, int op);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EVP_CipherUpdate(IntPtr ctx, byte[] output, ref int output_size, byte[] data, int len);

    }
}
