using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Shadowsocks.Controller
{
    class PolipoRunner
    {
        private Process process;

        public void Start(Server config)
        {
            if (process == null)
            {
                Process[] existingPolipo = Process.GetProcessesByName("ss_polipo");
                foreach (Process p in existingPolipo)
                {
                    p.Kill();
                    p.WaitForExit();
                }
                string temppath = Path.GetTempPath();
                string polipoConfig = Resources.polipo_config;
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", config.local_port.ToString());
                FileManager.ByteArrayToFile(temppath + "/polipo.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));
                FileManager.UncompressFile(temppath + "/ss_polipo.exe", Resources.polipo_exe);

                process = new Process();
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = temppath + "/ss_polipo.exe";
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
                try
                {
                    process.WaitForExit();
                }
                catch (InvalidOperationException)
                {
                    // do nothing
                }
                process = null;
            }
        }
    }
}
