using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.Stream
{
    public class StreamPlainNativeEncryptor : StreamEncryptor
    {

        public StreamPlainNativeEncryptor(string method, string password) : base(method, password)
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
        };

        public static Dictionary<string, CipherInfo> SupportedCiphers()
        {
            return _ciphers;
        }

        protected override Dictionary<string, CipherInfo> getCiphers()
        {
            return _ciphers;
        }
        #endregion
    }
}
