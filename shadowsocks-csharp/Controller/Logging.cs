using System;
using System.IO;
using System.Net.Sockets;

namespace Shadowsocks.Controller
{
    public class Logging
    {
        public static string LogFile;
        private static FileStream _logFileStream;
        private static StreamWriter _streamWriter;
        public static bool AttatchToConsole()
        {
            try
            {
                string temppath = Path.GetTempPath();
                LogFile = Path.Combine(temppath, "shadowsocks.log");
                if (_logFileStream == null)
                {
                    _logFileStream = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    _logFileStream.Seek(_logFileStream.Length, SeekOrigin.Begin);
                    _streamWriter = new StreamWriterWithTimestamp(_logFileStream) { AutoFlush = true };
                    Console.SetOut(_streamWriter);
                    Console.SetError(_streamWriter);
                }
                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public static void LogUsefulException(Exception e)
        {
            // just log useful exceptions, not all of them
            if (e is SocketException)
            {
                SocketException se = (SocketException)e;
                if (se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // closed by browser when sending
                    // normally happens when download is canceled or a tab is closed before page is loaded
                }
                else if (se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // received rst
                }
                else if (se.SocketErrorCode == SocketError.NotConnected)
                {
                    // close when not connected
                }
                else
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Clear the log file
        /// </summary>
        /// <returns></returns>
        public static bool Clear()
        {
            if (_logFileStream != null)
            {
                try
                {
                    _logFileStream.SetLength(0);
                    return true;
                }
                catch (IOException ioException)
                {
                    Console.WriteLine(ioException);
                }
            }
            return false;
        }
    }

    // Simply extended System.IO.StreamWriter for adding timestamp workaround
    public class StreamWriterWithTimestamp : StreamWriter
    {
        public StreamWriterWithTimestamp(Stream stream)
            : base(stream)
        {
        }

        private string GetTimestamp()
        {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(GetTimestamp() + value);
        }

        public override void Write(string value)
        {
            base.Write(GetTimestamp() + value);
        }
    }

}
