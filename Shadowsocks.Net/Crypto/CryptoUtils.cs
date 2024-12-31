using CryptoBase.Digests.MD5;

namespace Shadowsocks.Net.Crypto
{
    public static class CryptoUtils
    {
        public static byte[] MD5(byte[] b)
        {
            var hash = new byte[CryptoBase.MD5Length];
            using DefaultMD5Digest md5 = new();
            md5.UpdateFinal(b, hash);
            return hash;
        }
    }
}
