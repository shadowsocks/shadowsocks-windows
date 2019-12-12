using System;
using System.Security.Cryptography;

namespace Shadowsocks.Encryption
{
    public static class RNG
    {
        private static RNGCryptoServiceProvider _rng = null;

        public static void Init()
        {
            _rng = _rng ?? new RNGCryptoServiceProvider();
        }

        public static void Close()
        {
            _rng?.Dispose();
            _rng = null;
        }

        public static void Reload()
        {
            Close();
            Init();
        }

        public static void GetBytes(byte[] buf)
        {
            GetBytes(buf, buf.Length);
        }

        public static void GetBytes(byte[] buf, int len)
        {
            if (_rng == null) Init();
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