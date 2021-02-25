using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Protocol
{
    internal static class Util
    {
        private static readonly IdnMapping _idnMapping = new IdnMapping();

        public static string RestoreHostName(string punycode) => Encoding.UTF8.GetByteCount(punycode) != punycode.Length
                ? punycode.ToLowerInvariant()
                : _idnMapping.GetUnicode(punycode).ToLowerInvariant();

        public static string EncodeHostName(string unicode) => Encoding.UTF8.GetByteCount(unicode) != unicode.Length
                ? _idnMapping.GetAscii(unicode).ToLowerInvariant()
                : unicode.ToLowerInvariant();

        public static ArraySegment<byte> GetArray(ReadOnlyMemory<byte> m)
        {
            if (!MemoryMarshal.TryGetArray(m, out var arr))
            {
                throw new InvalidOperationException("Can't get base array");
            }
            return arr;
        }

        public static ArgumentException BufferTooSmall(int expected, int actual, string name) => new ArgumentException($"Require {expected} byte buffer, received {actual} byte", name);

        public static bool MemEqual(Memory<byte> m1, Memory<byte> m2)
        {
            if (m1.Length != m2.Length) return false;
            for (var i = 0; i < m1.Length; i++)
            {
                if (m1.Span[i] != m2.Span[i]) return false;
            }

            return true;
        }

        public static void SodiumIncrement(Span<byte> salt)
        {
            for (var i = 0; i < salt.Length; ++i)
            {
                if (++salt[i] != 0)
                {
                    break;
                }
            }
        }
    }
}
