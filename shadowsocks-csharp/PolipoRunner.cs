using shadowsocks_csharp.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

        public static void UncompressFile(string fileName, byte[] content)
        {
            FileStream destinationFile = File.Create(fileName);

            // Because the uncompressed size of the file is unknown, 
            // we are using an arbitrary buffer size.
            byte[] buffer = new byte[4096];
            int n;

            using (GZipStream input = new GZipStream(new MemoryStream(content),
                CompressionMode.Decompress, false))
            {
                while (true)
                {
                    n = input.Read(buffer, 0, buffer.Length);
                    if (n == 0)
                    {
                        break;
                    }
                    destinationFile.Write(buffer, 0, n);
                }
            }
            destinationFile.Close();
        }

        public void Start(Config config)
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
                ByteArrayToFile(temppath + "/polipo.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));
                UncompressFile(temppath + "/ss_polipo.exe", Resources.polipo_exe);

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
