using System;
using System.Collections.Generic;

namespace Shadowsocks.Net.Crypto.Stream
{
    public class StreamPlainNativeCrypto : StreamCrypto
    {

        public StreamPlainNativeCrypto(string method, string password) : base(method, password)
        {
        }

        protected override int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            cipher.CopyTo(plain);
            return cipher.Length;
        }

        protected override int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            plain.CopyTo(cipher);
            return plain.Length;
        }

        #region Cipher Info
        private static readonly Dictionary<string, CipherInfo> _ciphers = new Dictionary<string, CipherInfo>
        {
            {"plain", new CipherInfo("plain", 0, 0, CipherFamily.Plain) },
            {"none", new CipherInfo("none", 0, 0, CipherFamily.Plain) },
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

        public override void Dispose() { }
    }
}
