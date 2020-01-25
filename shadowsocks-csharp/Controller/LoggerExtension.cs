using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Text;
using Shadowsocks.Util.SystemProxy;

namespace NLog
{
    public static class LoggerExtension
    {
        public static void Dump(this Logger logger, string tag, byte[] arr, int length)
        {
            if (logger.IsTraceEnabled)
            {
                var sb = new StringBuilder($"{Environment.NewLine}{tag}: ");
                for (int i = 0; i < length - 1; i++)
                {
                    sb.Append($"0x{arr[i]:X2}, ");
                }
                sb.Append($"0x{arr[length - 1]:X2}");
                sb.Append(Environment.NewLine);
                logger.Trace(sb.ToString());
            }
        }

        public static void Debug(this Logger logger, EndPoint local, EndPoint remote, int len, string header = null, string tailer = null)
        {
            if (logger.IsDebugEnabled)
            {
                if (header == null && tailer == null)
                    logger.Debug($"{local} => {remote} (size={len})");
                else if (header == null && tailer != null)
                    logger.Debug($"{local} => {remote} (size={len}), {tailer}");
                else if (header != null && tailer == null)
                    logger.Debug($"{header}: {local} => {remote} (size={len})");
                else
                    logger.Debug($"{header}: {local} => {remote} (size={len}), {tailer}");
            }
        }

        public static void Debug(this Logger logger, Socket sock, int len, string header = null, string tailer = null)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(sock.LocalEndPoint, sock.RemoteEndPoint, len, header, tailer);
            }
        }

        public static void LogUsefulException(this Logger logger, Exception e)
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
                    logger.Warn(e);
                }
            }
            else if (e is ObjectDisposedException)
            {
            }
            else if (e is Win32Exception)
            {
                var ex = (Win32Exception)e;

                // Win32Exception (0x80004005): A 32 bit processes cannot access modules of a 64 bit process.
                if ((uint)ex.ErrorCode != 0x80004005)
                {
                    logger.Warn(e);
                }
            }
            else if (e is ProxyException)
            {
                var ex = (ProxyException)e;
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
            else
            {
                logger.Warn(e);
            }
        }
    }
}
