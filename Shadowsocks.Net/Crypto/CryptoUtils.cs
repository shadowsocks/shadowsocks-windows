using CryptoBase.Digests.MD5;

namespace Shadowsocks.Net.Crypto
{
    public static class CryptoUtils
    {
        public static byte[] MD5(byte[] b)
        {
            var hash = new byte[CryptoBase.MD5Length];
            MD5Utils.Default(b, hash);
            return hash;
        }
    }
}
