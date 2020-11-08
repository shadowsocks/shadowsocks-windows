using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;

namespace Shadowsocks.Net.Crypto.Stream
{
    public class StreamChachaBouncyCastleCrypto : StreamCrypto
    {
        private readonly BufferedCipherBase _encryptor;

        public StreamChachaBouncyCastleCrypto(string method, string password) : base(method, password)
        {
            _encryptor = new BufferedStreamCipher(new ChaCha7539Engine());
        }

        protected override void InitCipher(byte[] iv, bool isEncrypt)
        {
            base.InitCipher(iv, isEncrypt);
            _encryptor.Init(isEncrypt, new ParametersWithIV(new KeyParameter(key), iv));
        }

        protected override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            return CipherUpdate(plain, cipher);
        }

        protected override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            return CipherUpdate(cipher, plain);
        }

        protected virtual int CipherUpdate(ReadOnlySpan<byte> input, Span<byte> output)
        {
            var i = input.ToArray();
            var o = new byte[_encryptor.GetOutputSize(i.Length)];
            var res = _encryptor.ProcessBytes(i, 0, i.Length, o, 0);
            o.CopyTo(output);
            return res;
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            { "chacha20-ietf", new CipherInfo("chacha20-ietf", 32, 12, CipherFamily.Chacha20) },
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
