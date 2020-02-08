using Shadowsocks.Util.SystemProxy;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace NLog
{
    public static class LoggerExtension
    {
        public static void Dump(this Logger logger, string tag, byte[] arr, int length)
        {
            if (!logger.IsTraceEnabled)
            {
                return;
            }

            logger.Trace(Environment.NewLine
                + $"{tag}: {BitConverter.ToString(arr.Take(length).ToArray())}"
                + Environment.NewLine);
        }

        public static void Debug(this Logger logger, EndPoint local, EndPoint remote, int len, string header = null, string tailer = null)
        {
            if (!logger.IsDebugEnabled)
            {
                return;
            }

            string fullheader = header == null ? "" : header + ": ";
            string fulltailer = tailer == null ? "" : ", " + tailer;
            logger.Debug(fullheader + $"{local} => {remote} (size={len})" + fulltailer);

        }

        public static void Debug(this Logger logger, Socket sock, int len, string header = null, string tailer = null)
        {
            if (!logger.IsDebugEnabled)
            {
                return;
            }

            logger.Debug(sock.LocalEndPoint, sock.RemoteEndPoint, len, header, tailer);

        }

        public static void LogUsefulException(this Logger logger, Exception e)
        {
            // just log useful exceptions, not all of them
            if (e is SocketException)
            {
                SocketException se = (SocketException)e;

                switch (se.SocketErrorCode)
                {
                    // closed by browser when sending
                    // normally happens when download is canceled or a tab is closed before page is loaded
                    case SocketError.ConnectionAborted:
                    // received rst
                    case SocketError.ConnectionReset:
                    // The application tried to send or receive data, and the System.Net.Sockets.Socket is not connected.
                    case SocketError.NotConnected:
                    // There is no network route to the specified host.
                    case SocketError.HostUnreachable:
                    // The connection attempt timed out, or the connected host has failed to respond.
                    case SocketError.TimedOut:
                        break;
                    default:
                        logger.Warn(e);
                        break;
                }
            }
            else if (e is ObjectDisposedException)
            {
            }
            else if (e is Win32Exception)
            {
                Win32Exception ex = (Win32Exception)e;

                // Win32Exception (0x80004005): A 32 bit processes cannot access modules of a 64 bit process.
                // Why?
                if ((uint)ex.ErrorCode != 0x80004005)
                {
                    logger.Warn(e);
                }
            }
            else if (e is ProxyException)
            {
                ProxyException ex = (ProxyException)e;
                switch (ex.Type)
                {
                    case ProxyExceptionType.FailToRun:
                    case ProxyExceptionType.QueryReturnMalformed:
                    case ProxyExceptionType.SysproxyExitError:
                        logger.Error($"sysproxy - {ex.Type.ToString()}:{ex.Message}");
                        break;
                    case ProxyExceptionType.QueryReturnEmpty:
                    case ProxyExceptionType.Unspecific:
                        logger.Error($"sysproxy - {ex.Type.ToString()}");
                        break;
                }
            }
            else if (e is TargetInvocationException)
            {
                logger.LogUsefulException(e.InnerException);
            }
            else
            {
                logger.Warn(e);
            }
        }
    }
}
