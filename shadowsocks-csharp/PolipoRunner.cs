using shadowsocks_csharp.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace shadowsocks_csharp
{
    class PolipoRunner
    {
        private Process process;
        private bool ByteArrayToFile(string fileName, byte[] content)
        {
            try
            {
                System.IO.FileStream _FileStream =
                   new System.IO.FileStream(fileName, System.IO.FileMode.Create,
                                            System.IO.FileAccess.Write);
                _FileStream.Write(content, 0, content.Length);
                _FileStream.Close();
                return true;
            }
            catch (Exception _Exception)
            {
                Console.WriteLine("Exception caught in process: {0}",
                                  _Exception.ToString());
            }
            return false;
        }
        public void Start(Config config)
        {
            if (process == null)
            {
                string temppath = Path.GetTempPath();
                string polipoConfig = Resources.polipo_config;
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", config.local_port.ToString());
                ByteArrayToFile(temppath + "/polipo.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));
                ByteArrayToFile(temppath + "/polipo.exe", Resources.polipo);

                process = new Process();
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = temppath + "/polipo.exe";
                process.StartInfo.Arguments = "-c \"" + temppath + "/polipo.conf\"";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                //process.StandardOutput
                process.Start();
            }
        }

        public void Stop()
        {
            if (process != null)
            {
                process.Kill();
                process.WaitForExit();
                process = null;
            }
        }
    }
}
