using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Encryption
{
    public class LibcryptoEncryptor
        : IVEncryptor, IDisposable
    {
        const int CIPHER_AES = 1;
        const int CIPHER_RC4 = 2;
        const int CIPHER_CAMELLIA = 3;
        const int CIPHER_OTHER_CFB = 4;

        private IntPtr _encryptCtx = IntPtr.Zero;
        private IntPtr _decryptCtx = IntPtr.Zero;

        public LibcryptoEncryptor(string method, string password)
            : base(method, password)
        {
            InitKey(method, password);
        }

        private static Dictionary<string, int[]> _ciphers = new Dictionary<string, int[]> {
                //{"rc4", new int[]{16, 0, CIPHER_RC4}},
                {"rc4-md5", new int[]{16, 16, CIPHER_RC4}},
                {"aes-128-cfb", new int[]{16, 16, CIPHER_AES}},
                {"aes-192-cfb", new int[]{24, 16, CIPHER_AES}},
                {"aes-256-cfb", new int[]{32, 16, CIPHER_AES}},
                {"aes-128-ofb", new int[]{16, 16, CIPHER_AES}},
                {"aes-192-ofb", new int[]{24, 16, CIPHER_AES}},
                {"aes-256-ofb", new int[]{32, 16, CIPHER_AES}},
                {"camellia-128-cfb", new int[]{16, 16, CIPHER_CAMELLIA}},
                {"camellia-192-cfb", new int[]{24, 16, CIPHER_CAMELLIA}},
                {"camellia-256-cfb", new int[]{32, 16, CIPHER_CAMELLIA}},
                {"bf-cfb", new int[]{16, 8, CIPHER_OTHER_CFB}},
                {"cast5-cfb", new int[]{16, 8, CIPHER_OTHER_CFB}},
                {"idea-cfb", new int[]{16, 8, CIPHER_OTHER_CFB}},
                {"seed-cfb", new int[]{16, 16, CIPHER_OTHER_CFB}},
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        public static bool isSupport()
        {
            return Libcrypto.isSupport();
        }

        protected override Dictionary<string, int[]> getCiphers()
        {
            return _ciphers;
        }

        protected override void initCipher(byte[] iv, bool isCipher)
        {
            base.initCipher(iv, isCipher);

            IntPtr ctx;
            byte[] realkey;
            if (_method == "rc4-md5")
            {
                byte[] temp = new byte[keyLen + ivLen];
                realkey = new byte[keyLen];
                Array.Copy(_key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                realkey = MbedTLS.MbedTLSMD5(temp);
            }
            else
            {
                realkey = _key;
            }
            if (isCipher)
            {
                if (_encryptCtx == IntPtr.Zero)
                {
                    ctx = Libcrypto.init(Method, realkey, iv, 1);
                    _encryptCtx = ctx;
                }
                else
                {
                    ctx = _encryptCtx;
                }
            }
            else
            {
                if (_decryptCtx == IntPtr.Zero)
                {
                    ctx = Libcrypto.init(Method, realkey, iv, 0);
                    _decryptCtx = ctx;
                }
                else
                {
                    ctx = _decryptCtx;
                }
            }
        }
        protected override void cipherUpdate(bool isCipher, int length, byte[] buf, byte[] outbuf)
        {
            // C# could be multi-threaded
            if (_disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
            int len = Libcrypto.update(isCipher ? _encryptCtx : _decryptCtx, buf, length, outbuf);
        }

        #region IDisposable
        private bool _disposed;

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LibcryptoEncryptor()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            if (disposing)
            {
                if (_encryptCtx != IntPtr.Zero)
                    Libcrypto.clean(_encryptCtx);
                if (_decryptCtx != IntPtr.Zero)
                    Libcrypto.clean(_decryptCtx);
                _encryptCtx = IntPtr.Zero;
                _decryptCtx = IntPtr.Zero;
            }
        }
        #endregion
    }
}
