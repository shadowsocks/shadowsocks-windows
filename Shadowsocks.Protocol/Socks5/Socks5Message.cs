using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Shadowsocks.Protocol.Socks5
{
    public abstract class Socks5Message : IProtocolMessage
    {
        public abstract int Serialize(Memory<byte> buffer);
        public abstract (bool success, int length) TryLoad(ReadOnlyMemory<byte> buffer);

        public abstract bool Equals(IProtocolMessage other);

        #region Socks5 constants

        public const byte AuthNone = 0;
        public const byte AuthGssApi = 1;
        public const byte AuthUserPass = 2;
        public const byte AuthChallengeHandshake = 3;
        public const byte AuthChallengeResponse = 5;
        public const byte AuthSsl = 6;
        public const byte AuthNds = 7;
        public const byte AuthMultiAuthenticationFramework = 8;
        public const byte AuthJsonParameterBlock = 9;
        public const byte AuthNoAcceptable = 0xff;

        public const byte AddressIPv4 = 1;
        public const byte AddressDomain = 3;
        public const byte AddressIPv6 = 4;

        public const byte CmdConnect = 1;
        public const byte CmdBind = 2;
        public const byte CmdUdpAssociation = 3;

        public const byte ReplySucceed = 0;
        public const byte ReplyFailure = 1;
        public const byte ReplyNotAllowed = 2;
        public const byte ReplyNetworkUnreachable = 3;
        public const byte ReplyHostUnreachable = 4;
        public const byte ReplyConnectionRefused = 5;
        public const byte ReplyTtlExpired = 6;
        public const byte ReplyCommandNotSupport = 7;
        public const byte ReplyAddressNotSupport = 8;

        #endregion

        private static readonly NotSupportedException _addressNotSupport =
            new NotSupportedException("Socks5 only support IPv4, IPv6, Domain name address");

        #region Address convert

        private static (byte high, byte low) ExpandPort(int port)
        {
            Debug.Assert(port >= 0 && port <= 65535);
            return ((byte) (port / 256), (byte) (port % 256));
        }

        private static int TransformPort(byte high, byte low) => high * 256 + low;

        protected static int NeededBytes(EndPoint endPoint)
        {
            switch (endPoint)
            {
                case IPEndPoint ipEndPoint when ipEndPoint.AddressFamily == AddressFamily.InterNetwork:
                    return 7;

                case IPEndPoint ipEndPoint when ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6:
                    return 19;

                case DnsEndPoint dnsEndPoint:
                    var host = Util.EncodeHostName(dnsEndPoint.Host);
                    return host.Length + 4;

                default:
                    throw _addressNotSupport;
            }
        }

        public static int SerializeAddress(Memory<byte> buffer, EndPoint endPoint)
        {
            switch (endPoint)
            {
                case IPEndPoint ipEndPoint when ipEndPoint.AddressFamily == AddressFamily.InterNetwork:
                {
                    if (buffer.Length < 7) throw Util.BufferTooSmall(7, buffer.Length, nameof(buffer));
                    buffer.Span[0] = AddressIPv4;
                    Debug.Assert(ipEndPoint.Address.TryWriteBytes(buffer.Span[1..], out var l));
                    Debug.Assert(l == 4);

                    (var high, var low) = ExpandPort(ipEndPoint.Port);
                    buffer.Span[5] = high;
                    buffer.Span[6] = low;
                    return 7;
                }

                case IPEndPoint ipEndPoint when ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6:
                {
                    if (buffer.Length < 19) throw Util.BufferTooSmall(19, buffer.Length, nameof(buffer));
                    buffer.Span[0] = AddressIPv6;
                    Debug.Assert(ipEndPoint.Address.TryWriteBytes(buffer.Span[1..], out var l));
                    Debug.Assert(l == 16);

                    (var high, var low) = ExpandPort(ipEndPoint.Port);
                    buffer.Span[18] = low;
                    buffer.Span[17] = high;
                    return 19;
                }

                case DnsEndPoint dnsEndPoint:
                {
                    // 3 lHost [Host] port port

                    var host = Util.EncodeHostName(dnsEndPoint.Host);
                    if (host.Length > 255) throw new NotSupportedException("Host name too long");
                    if (buffer.Length < host.Length + 4)
                        throw Util.BufferTooSmall(host.Length + 4, buffer.Length, nameof(buffer));

                    buffer.Span[0] = AddressDomain;
                    buffer.Span[1] = (byte) host.Length;
                    Encoding.ASCII.GetBytes(host, buffer.Span[2..]);

                    (var high, var low) = ExpandPort(dnsEndPoint.Port);
                    buffer.Span[host.Length + 2] = high;
                    buffer.Span[host.Length + 3] = low;

                    return host.Length + 4;
                }

                default:
                    throw _addressNotSupport;
            }
        }

        public static (bool success, int length) TryParseAddress(ReadOnlyMemory<byte> buffer,
            out EndPoint result)
        {
            result = default;
            if (buffer.Length < 1) return (false, 1);
            var addrType = buffer.Span[0];
            int len;
            switch (addrType)
            {
                case AddressIPv4:
                    if (buffer.Length < 7) return (false, 7);
                    var s = buffer[1..5];
                    result = new IPEndPoint(
                        new IPAddress(Util.GetArray(s)),
                        TransformPort(buffer.Span[5], buffer.Span[6])
                    );
                    len = 7;
                    break;

                case AddressDomain:
                    if (buffer.Length < 2) return (false, 2);
                    var nameLength = buffer.Span[1];
                    if (buffer.Length < nameLength + 4) return (false, nameLength + 4);

                    result = new DnsEndPoint(
                        Encoding.ASCII.GetString(buffer.Span[2..(nameLength + 2)]),
                        TransformPort(buffer.Span[nameLength + 2], buffer.Span[nameLength + 3])
                    );
                    len = nameLength + 4;

                    break;

                case AddressIPv6:
                    if (buffer.Length < 19) return (false, 19);
                    result = new IPEndPoint(new IPAddress(Util.GetArray(buffer[1..17])),
                        TransformPort(buffer.Span[17], buffer.Span[18]));
                    len = 19;

                    break;

                default:
                    return (false, 0);
            }

            return (true, len);
        }

        #endregion
    }
}