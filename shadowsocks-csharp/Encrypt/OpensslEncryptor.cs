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
        };
        public OpensslEncryptor(string method, string password)
            : base(method, password)
        {
            InitKey(method, password);
        }

        public override byte[] Encrypt(byte[] buf, int length)
        {
            if (_encryptCtx == IntPtr.Zero)
            {
                byte[] iv = new byte[ivLen];
                OpenSSL.RAND_bytes(iv, iv.Length);
                InitCipher(ref _encryptCtx, iv, true);
                int outLen = length + ivLen;
                byte[] cipherText = new byte[outLen];
                OpenSSL.EVP_CipherUpdate(_encryptCtx, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen + ivLen];
                Buffer.BlockCopy(iv, 0, result, 0, ivLen);
                Buffer.BlockCopy(cipherText, 0, result, ivLen, outLen);
                return result;
            }
            else
            {
                int outLen = length + ivLen;
                byte[] cipherText = new byte[outLen];
                OpenSSL.EVP_CipherUpdate(_encryptCtx, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen];
                Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
        }

        public override byte[] Decrypt(byte[] buf, int length)
        {
            if (_decryptCtx == IntPtr.Zero)
            {
                byte[] iv = new byte[ivLen];
                Buffer.BlockCopy(buf, 0, iv, 0, ivLen);
                InitCipher(ref _decryptCtx, iv, false);
                int outLen = length + ivLen;
                outLen -= ivLen;
                byte[] cipherText = new byte[outLen];
                byte[] subset = new byte[length - ivLen];
                Buffer.BlockCopy(buf, ivLen, subset, 0, length - ivLen);
                OpenSSL.EVP_CipherUpdate(_decryptCtx, cipherText, out outLen, subset, length - ivLen);
                byte[] result = new byte[outLen];
                Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
            else
            {
                int outLen = length + ivLen;
                byte[] cipherText = new byte[outLen];
                OpenSSL.EVP_CipherUpdate(_decryptCtx, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen];
                Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
        }

        private static readonly Dictionary<string, byte[]> CachedKeys = new Dictionary<string, byte[]>();
        private byte[] _key;
        private IntPtr _encryptCtx;
        private IntPtr _decryptCtx;
        private IntPtr _cipher;
        private int keyLen;
        private int ivLen;

        private void InitKey(string method, string password)
        {
            OpenSSL.OpenSSL_add_all_ciphers();
            method = method.ToLower();
            string k = method + ":" + password;
            _cipher = OpenSSL.EVP_get_cipherbyname(System.Text.Encoding.UTF8.GetBytes(method));
            if (_cipher == null)
            {
                throw new Exception("method not found");
            }
            keyLen = ciphers[method][0];
            ivLen = ciphers[method][1];
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
            OpenSSL.EVP_CipherInit_ex(ctx, _cipher, IntPtr.Zero, _key, iv, enc);
        }

        #region IDisposable
        private bool _disposed;

        public void Dispose()
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

                if (_encryptCtx != IntPtr.Zero)
                {
                    OpenSSL.EVP_CIPHER_CTX_cleanup(_encryptCtx);
                    OpenSSL.EVP_CIPHER_CTX_free(_encryptCtx);
                    _encryptCtx = IntPtr.Zero;
                }
                if (_decryptCtx != IntPtr.Zero)
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
