using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Shadowsocks.Obfs;

namespace Shadowsocks.Controller
{
    public enum LogLevel
    {
        Debug = 0,
        Info,
        Warn,
        Error,
        Assert,
    }

    public class Logging
    {
        public static string LogFile;

        public static bool OpenLogFile()
        {
            try
            {
                string curpath = Path.Combine(System.Windows.Forms.Application.StartupPath, @"temp");// Path.GetFullPath(".");//Path.GetTempPath();
                if (!Directory.Exists(curpath))
                {
                    Directory.CreateDirectory(curpath);
                }
                LogFile = Path.Combine(curpath, "shadowsocks.log");
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
        private static string ToString(StackFrame[] stacks)
        {
            string result = string.Empty;
            foreach (StackFrame stack in stacks)
            {
                result += string.Format("{0}\r\n", stack.GetMethod().ToString());
            }
            return result;
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
                else if ((uint)se.SocketErrorCode == 0x80004005)
                {
                    // already closed
                }
                else if (se.SocketErrorCode == SocketError.Shutdown)
                {
                    // ignore
                }
                else
                {
                    Console.WriteLine(e);
//#if DEBUG
                    Console.WriteLine(ToString(new StackTrace().GetFrames()));
//#endif
                }
            }
            else
            {
                Console.WriteLine(e);
//#if DEBUG
                Console.WriteLine(ToString(new StackTrace().GetFrames()));
//#endif
            }
        }

        public static bool LogSocketException(string remarks, string server, Exception e)
        {
            // just log useful exceptions, not all of them
            if (e is ObfsException)
            {
                ObfsException oe = (ObfsException)e;
                Logging.Log(LogLevel.Error, "Proxy server [" + remarks + "(" + server + ")] "
                    + oe.Message);
                return true;
            }
            else if (e is ObjectDisposedException)
            {
                // ignore
                return true;
            }
            else if (e is SocketException)
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
                else if ((uint)se.SocketErrorCode == 0x80004005)
                {
                    // already closed
                    return true;
                }
                else if (se.ErrorCode == 11004)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "DNS lookup failed");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.HostNotFound)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "Host not found");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "connection refused");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.NetworkUnreachable)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "network unreachable");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    //Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                    //    + "connection timeout");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.Shutdown)
                {
                    return true;
                }
                else
                {
                    Logging.Log(LogLevel.Info, "Proxy server [" + remarks + "(" + server + ")] "
                        + Convert.ToString(se.SocketErrorCode) + ":" + se.Message);
//#if DEBUG
                    Console.WriteLine(ToString(new StackTrace().GetFrames()));
//#endif
                    return true;
                }
            }
            return false;
        }
        public static void Log(LogLevel level, String s)
        {
            String[] strMap = new String[5]{
                "Debug",
                "Info",
                "Warn",
                "Error",
                "Assert",
            };
            Console.WriteLine("[" + strMap[(int)level] + "]" + s);
        }

        public static void LogBin(LogLevel level, string info, byte[] data, int length)
        {
#if _DEBUG
            string s = "";
            for (int i = 0; i < length; ++i)
            {
                string fs = "0" + Convert.ToString(data[i], 16);
                s += " " + fs.Substring(fs.Length - 2, 2);
            }
            Log(level, info + s);
#endif
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
