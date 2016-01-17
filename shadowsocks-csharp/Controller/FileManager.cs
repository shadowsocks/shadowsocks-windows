using System;
using System.IO;
using System.IO.Compression;

namespace Shadowsocks.Controller
{
    public class FileManager
    {
        public static bool ByteArrayToFile(string fileName, byte[] content)
        {
            try
            {
                FileStream _FileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
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
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destinationFile.Write(buffer, 0, n);
                }
            }
            destinationFile.Close();
        }

    }
}
