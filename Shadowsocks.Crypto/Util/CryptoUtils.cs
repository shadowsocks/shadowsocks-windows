using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Security.Cryptography;
using System.Threading;

namespace Shadowsocks.Crypto
{
    public static class CryptoUtils
    {
        private static readonly ThreadLocal<MD5> Md5Hasher = new ThreadLocal<MD5>(System.Security.Cryptography.MD5.Create);

        public static byte[] MD5(byte[] b)
        {
            var hash = new byte[CryptoBase.MD5Length];
            Md5Hasher.Value.TryComputeHash(b, hash, out _);
            return hash;
        }
        // currently useless, just keep api same
        public static Span<byte> MD5(Span<byte> span)
        {
            Span<byte> hash = new byte[CryptoBase.MD5Length];
            Md5Hasher.Value.TryComputeHash(span, hash, out _);
            return hash;
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
