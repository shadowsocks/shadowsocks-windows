using shadowsocks_csharp.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace shadowsocks_csharp
{
    static class Program
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                string dllPath = tempPath + "/polarssl.dll";
                PolipoRunner.UncompressFile(dllPath, Resources.polarssl_dll);
                LoadLibrary(dllPath);

                FileStream fs = new FileStream("shadowsocks.log", FileMode.Append);
                TextWriter tmp = Console.Out;
                StreamWriter sw = new StreamWriter(fs);
                sw.AutoFlush = true;
                Console.SetOut(sw);
                Console.SetError(sw);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());


        }
    }
}
