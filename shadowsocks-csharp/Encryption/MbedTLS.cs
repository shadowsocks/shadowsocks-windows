using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Encryption
{
    public class MbedTLS
    {
        const string DLLNAME = "libsscrypto";

        static MbedTLS()
        {
            string tempPath = Path.GetTempPath();
            string dllPath = Path.Combine(System.Windows.Forms.Application.StartupPath, @"temp") + "/libsscrypto.dll";
            try
            {
                FileManager.UncompressFile(dllPath, Resources.libsscrypto_dll);
            }
            catch (IOException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            LoadLibrary(dllPath);
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        public const int MD5_CTX_SIZE = 88;

        public static byte[] MD5(byte[] input)
        {
            IntPtr ctx = Marshal.AllocHGlobal(MD5_CTX_SIZE);
            byte[] output = new byte[16];
            MbedTLS.md5_init(ctx);
            MbedTLS.md5_starts(ctx);
            MbedTLS.md5_update(ctx, input, (uint)input.Length);
            MbedTLS.md5_finish(ctx, output);
            MbedTLS.md5_free(ctx);
            Marshal.FreeHGlobal(ctx);
            return output;
        }

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void md5_init(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void md5_free(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void md5_starts(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void md5_update(IntPtr ctx, byte[] input, uint ilen);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void md5_finish(IntPtr ctx, byte[] output);

    }
}