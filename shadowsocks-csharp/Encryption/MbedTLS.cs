using System;
using System.IO;
using System.Runtime.InteropServices;

using Shadowsocks.Controller;
using Shadowsocks.Properties;
using Shadowsocks.Util;

namespace Shadowsocks.Encryption
{
    public class MbedTLS
    {
        const string DLLNAME = "libsscrypto";

        static MbedTLS()
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
                Console.WriteLine(e.ToString());
            }
            LoadLibrary(dllPath);
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        public const int MD5_CTX_SIZE = 88;

        public static byte[] MD5(byte[] input)
        {
            byte[] output = new byte[16];
            MbedTLS.md5(input, (uint)input.Length, output);
            return output;
        }

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static void md5(byte[] input, uint ilen, byte[] output);
    }
}
