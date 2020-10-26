using Shadowsocks.Common.SystemProxy;

using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;

namespace NLog
{
    public static class LoggerExtension
    {
        // for key, iv, etc...
        public static void Dump(this Logger logger, string tag, ReadOnlySpan<byte> arr)
        {
            logger.Dump(tag, arr.ToArray(), arr.Length);
        }
        public static void Dump(this Logger logger, string tag, byte[] arr, int length = -1)
        {
            if (arr == null) logger.Trace($@"
{tag}: 
(null)

");
            if (length == -1) length = arr.Length;

            if (!logger.IsTraceEnabled) return;
            string hex = BitConverter.ToString(arr.AsSpan(0, Math.Min(arr.Length, length)).ToArray()).Replace("-", "");
            string content = $@"
{tag}:
{hex}

";
            logger.Trace(content);
        }
        // for cipher and plain text, so we can use openssl to test
        public static void DumpBase64(this Logger logger, string tag, ReadOnlySpan<byte> arr)
        {
            logger.DumpBase64(tag, arr.ToArray(), arr.Length);
        }
        public static void DumpBase64(this Logger logger, string tag, byte[] arr, int length = -1)
        {
            if (arr == null) logger.Trace($@"
{tag}: 
(null)

");
            if (length == -1) length = arr.Length;

            if (!logger.IsTraceEnabled) return;
            string hex = Convert.ToBase64String(arr.AsSpan(0, Math.Min(arr.Length, length)).ToArray());
            string content = $@"
{tag}:
{hex}

";
            logger.Trace(content);
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
                logger.Debug(sock.LocalEndPoint, sock.RemoteEndPoint, len, header, tailer);

        }

        public static void LogUsefulException(this Logger logger, Exception e)
        {
            // just log useful exceptions, not all of them
            if (e is SocketException se)
            {
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
            else if (e is Win32Exception ex)
            {
                // Win32Exception (0x80004005): A 32 bit processes cannot access modules of a 64 bit process.
                if ((uint)ex.ErrorCode != 0x80004005)
                    logger.Warn(e);

            }
            else if (e is ProxyException pe)
            {
                switch (pe.Type)
                {
                    case ProxyExceptionType.FailToRun:
                    case ProxyExceptionType.QueryReturnMalformed:
                    case ProxyExceptionType.SysproxyExitError:
                        logger.Error($"sysproxy - {pe.Type}:{pe.Message}");
                        break;
                    case ProxyExceptionType.QueryReturnEmpty:
                    case ProxyExceptionType.Unspecific:
                        logger.Error($"sysproxy - {pe.Type}");
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
