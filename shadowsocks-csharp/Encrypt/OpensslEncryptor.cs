using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace shadowsocks_csharp.Encrypt
{
    public class OpensslEncryptor
        : EncryptorBase, IDisposable
    {
        static Dictionary<string, int[]> ciphers = new Dictionary<string, int[]> {
            {"aes-128-cfb", new int[]{16, 16}},
            {"aes-192-cfb", new int[]{24, 16}},
            {"aes-256-cfb", new int[]{32, 16}},
            {"bf-cfb", new int[]{16, 8}},
            {"rc4", new int[]{16, 0}},
            {"rc4-md5", new int[]{16, 16}},
        };

        static OpensslEncryptor()
        {
            OpenSSL.OpenSSL_add_all_ciphers();
        }

        public OpensslEncryptor(string method, string password)
            : base(method, password)
        {
            InitKey(method, password);
        }


        static byte[] tempbuf = new byte[32768];

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            if (_encryptCtx == IntPtr.Zero)
            {
                OpenSSL.RAND_bytes(outbuf, ivLen);
                InitCipher(ref _encryptCtx, outbuf, true);
                outlength = length + ivLen;
                OpenSSL.EVP_CipherUpdate(_encryptCtx, tempbuf, out outlength, buf, length);
                outlength = length + ivLen;
                Buffer.BlockCopy(tempbuf, 0, outbuf, ivLen, outlength);
            }
            else
            {
                outlength = length;
                OpenSSL.EVP_CipherUpdate(_encryptCtx, outbuf, out outlength, buf, length);
            }
        }

        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            if (_decryptCtx == IntPtr.Zero)
            {
                InitCipher(ref _decryptCtx, buf, false);
                outlength = length - ivLen;
                Buffer.BlockCopy(buf, ivLen, tempbuf, 0, length - ivLen);
                OpenSSL.EVP_CipherUpdate(_decryptCtx, outbuf, out outlength, tempbuf, length - ivLen);
            }
            else
            {
                outlength = length;
                OpenSSL.EVP_CipherUpdate(_decryptCtx, outbuf, out outlength, buf, length);
            }
        }

        private static readonly Dictionary<string, byte[]> CachedKeys = new Dictionary<string, byte[]>();
        private byte[] _key;
        private IntPtr _encryptCtx;
        private IntPtr _decryptCtx;
        private IntPtr _cipher;
        private string _method;
        private int keyLen;
        private int ivLen;

        private void InitKey(string method, string password)
        {
            method = method.ToLower();
            _method = method;
            string k = method + ":" + password;
            if (method == "rc4-md5")
            {
                method = "rc4";
            }
            _cipher = OpenSSL.EVP_get_cipherbyname(System.Text.Encoding.UTF8.GetBytes(method));
            if (_cipher == null)
            {
                throw new Exception("method not found");
            }
            keyLen = ciphers[_method][0];
            ivLen = ciphers[_method][1];
            if (CachedKeys.ContainsKey(k))
            {
                _key = CachedKeys[k];
            }
            else
            {
                byte[] passbuf = Encoding.UTF8.GetBytes(password);
                _key = new byte[32];
                byte[] iv = new byte[16];
                OpenSSL.EVP_BytesToKey(_cipher, OpenSSL.EVP_md5(), IntPtr.Zero, passbuf, passbuf.Length, 1, _key, iv);
                CachedKeys[k] = _key;
            }
        }

        private void InitCipher(ref IntPtr ctx, byte[] iv, bool isCipher)
        {
            ctx = OpenSSL.EVP_CIPHER_CTX_new();
            int enc = isCipher ? 1 : 0;
            byte[] realkey;
            IntPtr r = IntPtr.Zero;
            if (_method == "rc4-md5")
            {
                byte[] temp = new byte[keyLen + ivLen];
                realkey = new byte[keyLen];
                Array.Copy(_key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                r = OpenSSL.MD5(temp, keyLen + ivLen, null);
                Marshal.Copy(r, realkey, 0, keyLen);
            }
            else
            {
                realkey = _key;
            }
            OpenSSL.EVP_CipherInit_ex(ctx, _cipher, IntPtr.Zero, realkey, iv, enc);
        }

        #region IDisposable
        private bool _disposed;

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~OpensslEncryptor()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                }

                if (_encryptCtx.ToInt64() != 0)
                {
                    OpenSSL.EVP_CIPHER_CTX_cleanup(_encryptCtx);
                    OpenSSL.EVP_CIPHER_CTX_free(_encryptCtx);
                    _encryptCtx = IntPtr.Zero;
                }
                if (_decryptCtx.ToInt64() != 0)
                {
                    OpenSSL.EVP_CIPHER_CTX_cleanup(_decryptCtx);
                    OpenSSL.EVP_CIPHER_CTX_free(_decryptCtx);
                    _decryptCtx = IntPtr.Zero;
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
