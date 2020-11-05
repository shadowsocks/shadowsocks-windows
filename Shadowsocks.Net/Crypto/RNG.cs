using System;
using System.Security.Cryptography;

namespace Shadowsocks.Net.Crypto
{
    public static class RNG
    {
        private static RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();

        public static void Reload()
        {
            _rng.Dispose();
            _rng = new RNGCryptoServiceProvider();
        }

        public static void GetSpan(Span<byte> span)
        {
            _rng.GetBytes(span);
        }

        public static Span<byte> GetSpan(int length)
        {
            Span<byte> span = new byte[length];
            _rng.GetBytes(span);
            return span;
        }

        public static byte[] GetBytes(int length)
        {
            byte[] buf = new byte[length];
            _rng.GetBytes(buf);
            return buf;
        }

        public static void GetBytes(byte[] buf, int len)
        {
            try
            {
                _rng.GetBytes(buf, 0, len);
            }
            catch
            {
                // the backup way
                byte[] tmp = new byte[len];
                _rng.GetBytes(tmp);
                Buffer.BlockCopy(tmp, 0, buf, 0, len);
            }
        }
    }
}
