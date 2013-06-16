using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace shadowsocks_csharp
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {                
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
