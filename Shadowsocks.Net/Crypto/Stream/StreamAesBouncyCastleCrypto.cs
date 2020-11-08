using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Shadowsocks.Net.Crypto.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shadowsocks.Net.Crypto.Stream
{
    public class StreamAesCfbBouncyCastleCrypto : StreamCrypto
    {
        private readonly MyCfbBlockCipher b;
        public StreamAesCfbBouncyCastleCrypto(string method, string password) : base(method, password)
        {
            b = new MyCfbBlockCipher(new AesEngine(), 128);
        }

        protected override void InitCipher(byte[] iv, bool isEncrypt)
        {
            base.InitCipher(iv, isEncrypt);
            b.Init(isEncrypt, new ParametersWithIV(new KeyParameter(key), iv));
        }

        protected override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            return CipherUpdate(plain, cipher);
        }

        protected override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            return CipherUpdate(cipher, plain);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CipherUpdate(ReadOnlySpan<byte> input, Span<byte> output)
        {
            var i = input.ToArray();
            var o = new byte[i.Length];
            var res = b.ProcessBlock(i, 0, o, 0);
            o.CopyTo(output);
            return res;
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"aes-128-cfb",new CipherInfo("aes-128-cfb", 16, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
            {"aes-192-cfb",new CipherInfo("aes-192-cfb", 24, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
            {"aes-256-cfb",new CipherInfo("aes-256-cfb", 32, 16, CipherFamily.AesCfb, CipherStandardState.Unstable)},
        };

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }

        protected override Dictionary<string, CipherInfo> GetCiphers()
        {
            return _ciphers;
        }
        #endregion
    }
}
