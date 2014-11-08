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
        private Process _process;

        public void Start(Server config)
        {
            if (_process == null)
            {
                Process[] existingPolipo = Process.GetProcessesByName("ss_polipo");
                foreach (Process p in existingPolipo)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                string temppath = Path.GetTempPath();
                string polipoConfig = Resources.polipo_config;
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", config.local_port.ToString());
                FileManager.ByteArrayToFile(temppath + "/polipo.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));
                FileManager.UncompressFile(temppath + "/ss_polipo.exe", Resources.polipo_exe);

                _process = new Process();
                // Configure the process using the StartInfo properties.
                _process.StartInfo.FileName = temppath + "/ss_polipo.exe";
                _process.StartInfo.Arguments = "-c \"" + temppath + "/polipo.conf\"";
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.CreateNoWindow = true;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;
                //process.StandardOutput
                _process.Start();
            }
        }

        public void Stop()
        {
            if (_process != null)
            {
                try
                {
                    _process.Kill();
                    _process.WaitForExit();
                }
                catch (InvalidOperationException)
                {
                    // do nothing
                }
                _process = null;
            }
        }
    }
}
