using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Text;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    public class Logging
    {
        public static string LogFilePath;

        private static FileStream _fs;
        private static StreamWriterWithTimestamp _sw;

        public static bool OpenLogFile()
        {
            try
            {
                LogFilePath = Utils.GetTempPath("shadowsocks.log");

                _fs = new FileStream(LogFilePath, FileMode.Append);
                _sw = new StreamWriterWithTimestamp(_fs);
                _sw.AutoFlush = true;
                Console.SetOut(_sw);
                Console.SetError(_sw);

                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        private static void WriteToLogFile(object o)
        {
            try {
                Console.WriteLine(o);
            } catch(ObjectDisposedException) {
            }
        }

        public static void Error(object o)
        {
            WriteToLogFile("[E] " + o);
        }

        public static void Info(object o)
        {
            WriteToLogFile(o);
        }

        public static void Clear() {
            _sw.Close();
            _sw.Dispose();
            _fs.Close();
            _fs.Dispose();
            File.Delete(LogFilePath);
            OpenLogFile();
        }

        [Conditional("DEBUG")]
        public static void Debug(object o)
        {
            WriteToLogFile("[D] " + o);
        }

        [Conditional("DEBUG")]
        public static void Dump(string tag, byte[] arr, int length)
        {
            var sb = new StringBuilder($"{Environment.NewLine}{tag}: ");
            for (int i = 0; i < length - 1; i++) {
                sb.Append($"0x{arr[i]:X2}, ");
            }
            sb.Append($"0x{arr[length - 1]:X2}");
            sb.Append(Environment.NewLine);
            Debug(sb.ToString());
        }

        [Conditional("DEBUG")]
        public static void Debug(EndPoint local, EndPoint remote, int len, string header = null, string tailer = null)
        {
            if (header == null && tailer == null)
                Debug($"{local} => {remote} (size={len})");
            else if (header == null && tailer != null)
                Debug($"{local} => {remote} (size={len}), {tailer}");
            else if (header != null && tailer == null)
                Debug($"{header}: {local} => {remote} (size={len})");
            else
                Debug($"{header}: {local} => {remote} (size={len}), {tailer}");
        }

        [Conditional("DEBUG")]
        public static void Debug(Socket sock, int len, string header = null, string tailer = null)
        {
            Debug(sock.LocalEndPoint, sock.RemoteEndPoint, len, header, tailer);
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
            else if (e is Win32Exception)
            {
                var ex = (Win32Exception) e;

                // Win32Exception (0x80004005): A 32 bit processes cannot access modules of a 64 bit process.
                if ((uint) ex.ErrorCode != 0x80004005)
                {
                    Info(e);
                }
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
