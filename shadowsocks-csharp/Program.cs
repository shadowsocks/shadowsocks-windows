using shadowsocks_csharp.Controller;
using shadowsocks_csharp.Properties;
using shadowsocks_csharp.View;
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
                // TODO: PolipoRunner should not do this job
                FileManager.UncompressFile(dllPath, Resources.polarssl_dll);
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
            ShadowsocksController controller = new ShadowsocksController();

            // TODO run without a main form to save RAM
            Application.Run(new ConfigForm(controller));


        }
    }
}
