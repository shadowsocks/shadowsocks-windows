using Shadowsocks.Controller;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Encryption
{
    public class Sodium
    {
        const string DLLNAME = "libsscrypto";

        static Sodium()
        {
            string tempPath = Path.GetTempPath();
            string dllPath = tempPath + "/libsscrypto.dll";
            try
            {
                FileManager.UncompressFile(dllPath, Resources.libsscrypto_dll);
                LoadLibrary(dllPath);
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

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);
    }
}
