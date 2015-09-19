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
    public class Sodium
    {
        const string DLLNAME = "libsscrypto";

        static Sodium()
        {
            string tempPath = Utils.GetTempPath();
            string dllPath = tempPath + "/libsscrypto.dll";
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

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ss_gen_crc(byte[] buf, ref int buf_offset, ref int data_len,
            byte[] crc_buf, ref int crc_idx, int buf_size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public extern static int ss_onetimeauth(byte[] auth, 
            byte[] msg, int msg_len, 
            byte[] iv, int iv_len,
            byte[] key, int key_len);
    }
}

