using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    public static class CryptoUtils
    {
        private static readonly ThreadLocal<MD5> Md5Hasher = new ThreadLocal<MD5>(System.Security.Cryptography.MD5.Create);

        public static byte[] MD5(byte[] b)
        {
            var hash = new byte[16];
            Md5Hasher.Value?.TryComputeHash(b, hash, out _);
            return hash;
        }
        // currently useless, just keep api same
        public static Span<byte> MD5(Span<byte> span)
        {
            Span<byte> hash = new byte[16];
            Md5Hasher.Value?.TryComputeHash(span, hash, out _);
            return hash;
        }

        public static byte[] HKDF(int keylen, byte[] master, byte[] salt, byte[] info)
        {
            var ret = new byte[keylen];
            var degist = new Sha1Digest();
            var parameters = new HkdfParameters(master, salt, info);
            var hkdf = new HkdfBytesGenerator(degist);
            hkdf.Init(parameters);
            hkdf.GenerateBytes(ret, 0, keylen);
            return ret;
        }
        // currently useless, just keep api same, again
        public static Span<byte> HKDF(int keylen, Span<byte> master, Span<byte> salt, Span<byte> info)
        {
            var ret = new byte[keylen];
            var degist = new Sha1Digest();
            var parameters = new HkdfParameters(master.ToArray(), salt.ToArray(), info.ToArray());
            var hkdf = new HkdfBytesGenerator(degist);
            hkdf.Init(parameters);
            hkdf.GenerateBytes(ret, 0, keylen);
            return ret.AsSpan();
        }

        public static byte[] SSKDF(string password, int keylen)
        {
            var key = new byte[keylen];
            var pw = Encoding.UTF8.GetBytes(password);
            var result = new byte[password.Length + 16];
            var i = 0;
            var md5sum = Array.Empty<byte>();
            while (i < keylen)
            {
                if (i == 0)
                {
                    md5sum = MD5(pw);
                }
                else
                {
                    Array.Copy(md5sum, 0, result, 0, 16);
                    Array.Copy(pw, 0, result, 16, password.Length);
                    md5sum = MD5(result);
                }
                Array.Copy(md5sum, 0, key, i, Math.Min(16, keylen - i));
                i += 16;
            }
            return key;
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

        public static void RandomSpan(Span<byte> span)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(span);
            }
        }
    }
}
