#nullable enable
using CryptoBase;
using CryptoBase.Abstractions.SymmetricCryptos;
using CryptoBase.Digests.MD5;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shadowsocks.Net.Crypto.Stream
{
    public class StreamCryptoBaseCrypto : StreamCrypto
    {
        private IStreamCrypto? _crypto;

        public StreamCryptoBaseCrypto(string method, string password) : base(method, password)
        {
        }

        protected override void InitCipher(byte[] iv, bool isEncrypt)
        {
            base.InitCipher(iv, isEncrypt);
            _crypto?.Dispose();

            if (cipherFamily == CipherFamily.Rc4Md5)
            {
                Span<byte> temp = stackalloc byte[keyLen + ivLen];
                var realKey = new byte[MD5Length];
                key.CopyTo(temp);
                iv.CopyTo(temp.Slice(keyLen));
                MD5Utils.Fast440(temp, realKey);

                _crypto = StreamCryptoCreate.Rc4(realKey);
                return;
            }

            _crypto = cipherFamily switch
            {
                CipherFamily.AesCfb => StreamCryptoCreate.AesCfb(isEncrypt, key, iv),
                CipherFamily.Chacha20 => StreamCryptoCreate.ChaCha20(key, iv),
                CipherFamily.Rc4 => StreamCryptoCreate.Rc4(key),
                _ => throw new NotSupportedException()
            };
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
            _crypto!.Update(input, output);
            return input.Length;
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new()
        {
            { "aes-128-cfb", new CipherInfo("aes-128-cfb", 16, 16, CipherFamily.AesCfb, CipherStandardState.Unstable) },
            { "aes-192-cfb", new CipherInfo("aes-192-cfb", 24, 16, CipherFamily.AesCfb, CipherStandardState.Unstable) },
            { "aes-256-cfb", new CipherInfo("aes-256-cfb", 32, 16, CipherFamily.AesCfb, CipherStandardState.Unstable) },
            { "chacha20-ietf", new CipherInfo("chacha20-ietf", 32, 12, CipherFamily.Chacha20) },
            { "rc4", new CipherInfo("rc4", 16, 0, CipherFamily.Rc4) },
            { "rc4-md5", new CipherInfo("rc4-md5", 16, 16, CipherFamily.Rc4Md5) },
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

        public override void Dispose()
        {
            _crypto?.Dispose();
        }
    }
}
