using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shadowsocks.Controller
{
    public class Logging
    {
        public static string LogFile;

        public static bool OpenLogFile()
        {
            try
            {
                string temppath = Path.GetTempPath();
                LogFile = Path.Combine(temppath, "shadowsocks.log");
                FileStream fs = new FileStream(LogFile, FileMode.Append);
                TextWriter tmp = Console.Out;
                StreamWriter sw = new StreamWriter(fs);
                sw.AutoFlush = true;
                Console.SetOut(sw);
                Console.SetError(sw);
                
                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
    }
}
