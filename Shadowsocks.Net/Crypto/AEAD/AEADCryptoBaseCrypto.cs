#nullable enable
using CryptoBase;
using CryptoBase.Abstractions.SymmetricCryptos;
using System;
using System.Collections.Generic;

namespace Shadowsocks.Net.Crypto.AEAD
{
    public class AEADCryptoBaseCrypto : AEADCrypto
    {
        private IAEADCrypto? _crypto;

        public AEADCryptoBaseCrypto(string method, string password) : base(method, password)
        {
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new()
        {
            { "aes-128-gcm", new CipherInfo("aes-128-gcm", 16, 16, 12, 16, CipherFamily.AesGcm) },
            { "aes-192-gcm", new CipherInfo("aes-192-gcm", 24, 24, 12, 16, CipherFamily.AesGcm) },
            { "aes-256-gcm", new CipherInfo("aes-256-gcm", 32, 32, 12, 16, CipherFamily.AesGcm) },
            { "chacha20-ietf-poly1305", new CipherInfo("chacha20-ietf-poly1305", 32, 32, 12, 16, CipherFamily.Chacha20Poly1305) },
            { "xchacha20-ietf-poly1305", new CipherInfo("xchacha20-ietf-poly1305", 32, 32, 24, 16, CipherFamily.XChacha20Poly1305) },
        };

        protected override Dictionary<string, CipherInfo> GetCiphers()
        {
            return _ciphers;
        }

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }
        #endregion

        public override void InitCipher(byte[] salt, bool isEncrypt)
        {
            base.InitCipher(salt, isEncrypt);
            _crypto?.Dispose();

            _crypto = cipherFamily switch
            {
                CipherFamily.AesGcm => AEADCryptoCreate.AesGcm(sessionKey),
                CipherFamily.Chacha20Poly1305 => AEADCryptoCreate.ChaCha20Poly1305(sessionKey),
                CipherFamily.XChacha20Poly1305 => AEADCryptoCreate.XChaCha20Poly1305(sessionKey),
                _ => throw new NotSupportedException()
            };
        }

        public override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            _crypto!.Encrypt(nonce, plain, cipher.Slice(0, plain.Length), cipher.Slice(plain.Length, tagLen));
            return plain.Length + tagLen;
        }

        public override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            var clen = cipher.Length - tagLen;
            var ciphertxt = cipher.Slice(0, clen);
            var tag = cipher.Slice(clen);
            _crypto!.Decrypt(nonce, ciphertxt, tag, plain.Slice(0, clen));
            return clen;
        }

        public override void Dispose()
        {
            _crypto?.Dispose();
        }
    }
}
