using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Shadowsocks.Controller
{
    public class FileManager
    {
        public static bool ByteArrayToFile(string fileName, byte[] content)
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
    }
}
