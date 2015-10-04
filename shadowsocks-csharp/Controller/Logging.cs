using Shadowsocks.Util;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Shadowsocks.Controller
{
    public class Logging
    {
        public static string LogFile = Path.Combine(Utils.GetTempPath(), "shadowsocks.log");

        private static string TrafficLogFile = Path.Combine(Application.StartupPath, "shadowsocks-tr.log");

        private static StreamWriter TrLogStream = null;

        private static StreamWriter GetLogStream()
        {
            if(TrLogStream == null)
            {
                TrLogStream = new StreamWriter(File.Open(TrafficLogFile, FileMode.Append, FileAccess.Write, FileShare.Read));

                TrLogStream.WriteLine("New round of net traffic logging:" + DateTime.Now.ToString());

                Application.ApplicationExit += Cleanup;
            }

            return TrLogStream;
        }
                
        public static bool OpenLogFile()
        {
            try
            {
                FileStream fs = new FileStream(LogFile, FileMode.Append);
                StreamWriterWithTimestamp sw = new StreamWriterWithTimestamp(fs);
                sw.AutoFlush = true;
                Console.SetOut(sw);
                Console.SetError(sw);

                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public static void Debug(object o)
        {

#if DEBUG
            Console.WriteLine(o);
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
                    // close when not connected
                }
                else
                {
                    Console.WriteLine(e);
                }
            }
            else if (e is ObjectDisposedException)
            {
            }
            else
            {
                Console.WriteLine(e);
            }
        }

        public static void LogNetTraffic(string msg)
        {
            GetLogStream().WriteLine(StreamWriterWithTimestamp.GetTimestamp() + msg);
        }

        public static void LogNetTraffic(byte[] msgbin)
        {
            string msg = Hex.EncodeHexString(msgbin);
            GetLogStream().WriteLine(StreamWriterWithTimestamp.GetTimestamp() + msg);
        }

        static void Cleanup(object sender, System.EventArgs arg)
        {
            if(TrLogStream != null)
                TrLogStream.Dispose();
        }
    }

    // Simply extended System.IO.StreamWriter for adding timestamp workaround
    public class StreamWriterWithTimestamp : StreamWriter
    {
        public StreamWriterWithTimestamp(Stream stream) : base(stream)
        {
        }

        public static string GetTimestamp()
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
