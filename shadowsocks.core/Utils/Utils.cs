using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks
{
    public static class Utils
    {
        public static string TempPath { get; }
        static Utils()
        {
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "shadowsocks_portable_mode.txt")))
                try
                {
                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "temp"));
                }
                catch (Exception e)
                {
                    TempPath = Path.GetTempPath();
                    Logging.LogUsefulException(e);
                }
                finally
                {
                    // don't use "/", it will fail when we call explorer /select xxx/temp\xxx.log
                    TempPath = Path.Combine(Environment.CurrentDirectory, "temp");
                }
            else
                TempPath = Path.GetTempPath();
        }

        public static string GetTempPath(string filename)
        {
            return Path.Combine(TempPath, filename);
        }

        public static void ReleaseMemory(bool removePages)
        {
            // release any unused pages
            // making the numbers look good in task manager
            // this is totally nonsense in programming
            // but good for those users who care
            // making them happier with their everyday life
            // which is part of user experience
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            if (removePages)
            {
                // as some users have pointed out
                // removing pages from working set will cause some IO
                // which lowered user experience for another group of users
                //
                // so we do 2 more things here to satisfy them:
                // 1. only remove pages once when configuration is changed
                // 2. add more comments here to tell users that calling
                //    this function will not be more frequent than
                //    IM apps writing chat logs, or web browsers writing cache files
                //    if they're so concerned about their disk, they should
                //    uninstall all IM apps and web browsers
                //
                // please open an issue if you're worried about anything else in your computer
                // no matter it's GPU performance, monitor contrast, audio fidelity
                // or anything else in the task manager
                // we'll do as much as we can to help you
                //
                // just kidding
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (UIntPtr)0xFFFFFFFF, (UIntPtr)0xFFFFFFFF);
            }
        }

        public static string UnGzip(byte[] buf)
        {
            byte[] buffer = new byte[1024];
            int n;
            using (MemoryStream sb = new MemoryStream())
            {
                using (GZipStream input = new GZipStream(new MemoryStream(buf),
                    CompressionMode.Decompress, false))
                {
                    while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Write(buffer, 0, n);
                    }
                }
                return Encoding.UTF8.GetString(sb.ToArray());
            }
        }

        public static string FormatBandwidth(long n)
        {
            float f = n;
            string unit = "B";
            if (f > 1024)
            {
                f = f / 1024;
                unit = "KiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                unit = "MiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                unit = "GiB";
            }
            if (f > 1024)
            {
                f = f / 1024;
                unit = "TiB";
            }
            return $"{f:0.##}{unit}";
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);

        public static void UncompressFile(string fileName, byte[] content)
        {
            FileStream destinationFile = File.Create(fileName);

            // Because the uncompressed size of the file is unknown, 
            // we are using an arbitrary buffer size.
            byte[] buffer = new byte[4096];
            int n;

            using (GZipStream input = new GZipStream(new MemoryStream(content),CompressionMode.Decompress, false))
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

        public static bool IsWhiteSpace(this string value)
        {
            return value.All(char.IsWhiteSpace);
        }

        public static IEnumerable<string> NonWhiteSpaceLines(this TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.IsWhiteSpace()) continue;
                yield return line;
            }
        }
        public static bool BeginWithAny(this string s, IEnumerable<char> chars)
        {
            return !string.IsNullOrEmpty(s) && chars.Contains(s[0]);
        }
    }

    #region Log
    public static class Logging
    {
        public static string LogFilePath;

        static Logging()
        {
            try
            {
                LogFilePath = Utils.GetTempPath("shadowsocks.log");

                FileStream fs = new FileStream(LogFilePath, FileMode.Append);
                StreamWriterWithTimestamp sw = new StreamWriterWithTimestamp(fs) { AutoFlush = true };
                Console.SetOut(sw);
                Console.SetError(sw);

#if DEBUG
                // truncate privoxy log file while debugging
                string privoxyLogFilename = Utils.GetTempPath("privoxy.log");
                if (File.Exists(privoxyLogFilename))
                    using (new FileStream(privoxyLogFilename, FileMode.Truncate)) { }
#endif
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void WriteToLogFile(object o)
        {
            Console.WriteLine(o);
        }

        public static void Error(object o)
        {
            WriteToLogFile("[E] " + o);
        }

        public static void Info(object o)
        {
            WriteToLogFile(o);
        }

        public static void Debug(object o)
        {
#if DEBUG
            WriteToLogFile("[D] " + o);
#endif
        }

        public static void Debug(EndPoint local, EndPoint remote, int len, string header = null, string tailer = null)
        {
#if DEBUG
            if (header == null && tailer == null)
                Debug($"{local} => {remote} (size={len})");
            else if (header == null)
                Debug($"{local} => {remote} (size={len}), {tailer}");
            else if (tailer == null)
                Debug($"{header}: {local} => {remote} (size={len})");
            else
                Debug($"{header}: {local} => {remote} (size={len}), {tailer}");
#endif
        }

        public static void Debug(Socket sock, int len, string header = null, string tailer = null)
        {
#if DEBUG
            Debug(sock.LocalEndPoint, sock.RemoteEndPoint, len, header, tailer);
#endif
        }

        public static void LogUsefulException(Exception e)
        {
            // just log useful exceptions, not all of them
            var exception = e as SocketException;
            if (exception != null)
            {
                SocketException se = exception;
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
                    // The application tried to send or receive data, and the System.Net.Sockets.Socket is not connected.
                }
                else if (se.SocketErrorCode == SocketError.HostUnreachable)
                {
                    // There is no network route to the specified host.
                }
                else if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    // The connection attempt timed out, or the connected host has failed to respond.
                }
                else
                {
                    Info(exception);
                }
            }
            else if (e is ObjectDisposedException)
            {
            }
            else
            {
                Info(e);
            }
        }
    }

    // Simply extended System.IO.StreamWriter for adding timestamp workaround
    public class StreamWriterWithTimestamp : StreamWriter
    {
        public StreamWriterWithTimestamp(Stream stream) : base(stream)
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

#endregion
}
