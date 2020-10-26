using System;
using System.Collections.Generic;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Shadowsocks.Crypto.AEAD
{
    public class AEADBouncyCastleCrypto : AEADCrypto
    {
        IAeadCipher aead;
        bool enc;
        public AEADBouncyCastleCrypto(string method, string password) : base(method, password)
        {
            aead = cipherFamily switch
            {
                CipherFamily.AesGcm => new GcmBlockCipher(new AesEngine()),
                CipherFamily.Chacha20Poly1305 => new ChaCha20Poly1305(),
                _ => throw new NotSupportedException(),
            };
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"aes-128-gcm", new CipherInfo("aes-128-gcm", 16, 16, 12, 16, CipherFamily.AesGcm)},
            {"aes-192-gcm", new CipherInfo("aes-192-gcm", 24, 24, 12, 16, CipherFamily.AesGcm)},
            {"aes-256-gcm", new CipherInfo("aes-256-gcm", 32, 32, 12, 16, CipherFamily.AesGcm)},
            {"chacha20-ietf-poly1305", new CipherInfo("chacha20-ietf-poly1305",32, 32, 12, 16, CipherFamily.Chacha20Poly1305)},
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
            enc = isEncrypt;
        }

        public override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            return CipherUpdate(cipher, plain);
        }

        public override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            return CipherUpdate(plain, cipher);
        }

        private int CipherUpdate(ReadOnlySpan<byte> i, Span<byte> o)
        {
            byte[] t = new byte[o.Length];
            byte[] n = new byte[nonce.Length];
            nonce.CopyTo(n, 0);
            aead.Init(enc, new AeadParameters(new KeyParameter(sessionKey), tagLen * 8, n));

            int r = aead.ProcessBytes(i.ToArray(), 0, i.Length, t, 0);
            r += aead.DoFinal(t, r);
            t.CopyTo(o);
            return r;
        }
    }
}
