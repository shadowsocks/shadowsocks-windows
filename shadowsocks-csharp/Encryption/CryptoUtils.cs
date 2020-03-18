using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace Shadowsocks.Encryption
{
    public static class CryptoUtils
    {
        public static byte[] MD5(byte[] b)
        {
            MD5Digest md5 = new MD5Digest();
            md5.BlockUpdate(b, 0, b.Length);
            byte[] r = new byte[16];
            md5.DoFinal(r, 0);
            return r;
        }

        public static byte[] HKDF(int keylen, byte[] master, byte[] salt, byte[] info)
        {
            byte[] ret = new byte[keylen];
            IDigest degist = new Sha1Digest();
            HkdfParameters parameters = new HkdfParameters(master, salt, info);
            HkdfBytesGenerator hkdf = new HkdfBytesGenerator(degist);
            hkdf.Init(parameters);
            hkdf.GenerateBytes(ret, 0, keylen);
            return ret;
        }

        public static void SodiumIncrement(byte[] salt)
        {
            bool o = true; // overflow flag
            for (int i = 0; i < salt.Length; i++)
            {
                if (!o) continue;

                salt[i]++;
                o = salt[i] == 0;
            }
        }
    }
}
