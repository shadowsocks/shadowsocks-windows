using System;
using System.IO;
using System.Net.Sockets;
using System.Net;

using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    public class Logging
    {
        public static string LogFilePath;
        private static DateTime LogFileCreationTime;

        public static bool OpenLogFile()
        {
            try
            {
                LogFilePath = Utils.GetTempPath("shadowsocks.log");

                if (!File.Exists(LogFilePath))
                    using (File.Create(LogFilePath)) { }
                LogFileCreationTime = File.GetCreationTime(LogFilePath);

                if ((DateTime.Now - LogFileCreationTime).Days >= 1)
                    RollLogFile();
                else
                {
                    FileStream fs = new FileStream(LogFilePath, FileMode.Append);
                    StreamWriterWithTimestamp sw = new StreamWriterWithTimestamp(fs);
                    sw.AutoFlush = true;
                    Console.SetOut(sw);
                    Console.SetError(sw);
                }

                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        private static void RollLogFile()
        {
            Console.Out.Close();
            Console.Error.Close();

            MemoryStream ms = new MemoryStream();
            StreamWriterWithTimestamp sw = new StreamWriterWithTimestamp(ms);
            sw.AutoFlush = true;
            Console.SetOut(sw);
            Console.SetError(sw);

            byte[] logContents = File.ReadAllBytes(LogFilePath);
            string datestr = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            string filepath = Utils.GetTempPath($"shadowsocks.{datestr}.log.zip");
            FileManager.CompressFile(filepath, logContents);

            File.Delete(LogFilePath);
            FileStream fs = new FileStream(LogFilePath, FileMode.CreateNew);
            LogFileCreationTime = DateTime.Now;
            ms.CopyTo(fs);
            StreamWriterWithTimestamp sw2 = new StreamWriterWithTimestamp(fs);
            sw2.AutoFlush = true;
            Console.SetOut(sw2);
            Console.SetError(sw2);
        }

        private static void WriteToLogFile(object o)
        {
            if ((DateTime.Now - LogFileCreationTime).Days >= 1)
                RollLogFile();
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
            else if (header == null && tailer != null)
                Debug($"{local} => {remote} (size={len}), {tailer}");
            else if (header != null && tailer == null)
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
                    Info(e);
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

}
