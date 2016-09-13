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

        public static void InitAviable()
        {
            List<string> remove_ciphers = new List<string>();
            foreach (string cipher in _ciphers.Keys)
            {
                if (!Libcrypto.is_cipher(cipher))
                {
                    remove_ciphers.Add(cipher);
                }
            }
            foreach (string cipher in remove_ciphers)
            {
                _ciphers.Remove(cipher);
            }
        }

        private static Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo> {
                {"aes-128-cfb", new EncryptorInfo(16, 16, true, CIPHER_AES)},
                {"aes-192-cfb", new EncryptorInfo(24, 16, true, CIPHER_AES)},
                {"aes-256-cfb", new EncryptorInfo(32, 16, true, CIPHER_AES)},
                {"aes-128-ctr", new EncryptorInfo(16, 16, true, CIPHER_AES)},
                {"aes-192-ctr", new EncryptorInfo(24, 16, true, CIPHER_AES)},
                {"aes-256-ctr", new EncryptorInfo(32, 16, true, CIPHER_AES)},
                {"camellia-128-cfb", new EncryptorInfo(16, 16, true, CIPHER_CAMELLIA)},
                {"camellia-192-cfb", new EncryptorInfo(24, 16, true, CIPHER_CAMELLIA)},
                {"camellia-256-cfb", new EncryptorInfo(32, 16, true, CIPHER_CAMELLIA)},
                {"bf-cfb", new EncryptorInfo(16, 8, true, CIPHER_OTHER_CFB)},
                {"cast5-cfb", new EncryptorInfo(16, 8, true, CIPHER_OTHER_CFB)},
                //{"des-cfb", new EncryptorInfo(8, 8, true, CIPHER_OTHER_CFB)}, // weak
                //{"des-ede3-cfb", new EncryptorInfo(24, 8, true, CIPHER_OTHER_CFB)},
                {"idea-cfb", new EncryptorInfo(16, 8, true, CIPHER_OTHER_CFB)},
                {"rc2-cfb", new EncryptorInfo(16, 8, true, CIPHER_OTHER_CFB)},
                {"rc4", new EncryptorInfo(16, 0, false, CIPHER_RC4)}, // weak
                {"rc4-md5", new EncryptorInfo(16, 16, true, CIPHER_RC4)}, // weak
                {"rc4-md5-6", new EncryptorInfo(16, 6, true, CIPHER_RC4)}, // weak
                {"seed-cfb", new EncryptorInfo(16, 16, true, CIPHER_OTHER_CFB)},
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        public static bool isSupport()
        {
            return Libcrypto.isSupport();
        }

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        protected override void initCipher(byte[] iv, bool isCipher)
        {
            base.initCipher(iv, isCipher);

            IntPtr ctx;
            byte[] realkey;
            if (_method.StartsWith("rc4-md5"))
            {
                byte[] temp = new byte[keyLen + ivLen];
                realkey = new byte[keyLen];
                Array.Copy(_key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                realkey = MbedTLS.MD5(temp);
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
