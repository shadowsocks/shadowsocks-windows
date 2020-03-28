using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System;

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
        // currently useless, just keep api same
        public static Span<byte> MD5(Span<byte> span)
        {
            byte[] b = span.ToArray();
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
        // currently useless, just keep api same, again
        public static Span<byte> HKDF(int keylen, Span<byte> master, Span<byte> salt, Span<byte> info)
        {
            byte[] ret = new byte[keylen];
            IDigest degist = new Sha1Digest();
            HkdfParameters parameters = new HkdfParameters(master.ToArray(), salt.ToArray(), info.ToArray());
            HkdfBytesGenerator hkdf = new HkdfBytesGenerator(degist);
            hkdf.Init(parameters);
            hkdf.GenerateBytes(ret, 0, keylen);
            return ret.AsSpan();
        }

        public static void SodiumIncrement(byte[] salt)
        {
            bool o = true; // overflow flag
            for (int i = 0; i < salt.Length; i++)
            {
                if (!o)
                {
                    continue;
                }

                salt[i]++;
                o = salt[i] == 0;
            }
        }

        public static void SodiumIncrement(Span<byte> salt)
        {
            bool o = true; // overflow flag
            for (int i = 0; i < salt.Length; i++)
            {
                if (!o)
                {
                    continue;
                }

                salt[i]++;
                o = salt[i] == 0;
            }
        }
    }
}
