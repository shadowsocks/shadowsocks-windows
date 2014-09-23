using OpenSSL.Core;
using OpenSSL.Crypto;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace shadowsocks_csharp.Encrypt
{
    public class OpensslEncryptor
        : EncryptorBase, IDisposable
    {
        public OpensslEncryptor(string method, string password)
            : base(method, password)
        {
            InitKey(method, password);
        }

        public override byte[] Encrypt(byte[] buf, int length)
        {
            if (_encryptCtx == IntPtr.Zero)
            {
                int ivLen = _cipher.IVLength;
                byte[] iv = new byte[ivLen];
                Native.RAND_bytes(iv, iv.Length);
                InitCipher(ref _encryptCtx, iv, true);
                int outLen = length + _cipher.BlockSize;
                byte[] cipherText = new byte[outLen];
                Native.EVP_CipherUpdate(_encryptCtx, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen + ivLen];
                Buffer.BlockCopy(iv, 0, result, 0, ivLen);
                Buffer.BlockCopy(cipherText, 0, result, ivLen, outLen);
                return result;
            }
            else
            {
                int outLen = length + _cipher.BlockSize;
                byte[] cipherText = new byte[outLen];
                Native.EVP_CipherUpdate(_encryptCtx, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen];
                Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
        }

        public override byte[] Decrypt(byte[] buf, int length)
        {
            if (_decryptCtx == IntPtr.Zero)
            {
                int ivLen = _cipher.IVLength;
                byte[] iv = new byte[ivLen];
                Buffer.BlockCopy(buf, 0, iv, 0, ivLen);
                InitCipher(ref _decryptCtx, iv, false);
                int outLen = length + _cipher.BlockSize;
                outLen -= ivLen;
                byte[] cipherText = new byte[outLen];
                byte[] subset = new byte[length - ivLen];
                Buffer.BlockCopy(buf, ivLen, subset, 0, length - ivLen);
                Native.EVP_CipherUpdate(_decryptCtx, cipherText, out outLen, subset, length - ivLen);
                byte[] result = new byte[outLen];
                Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
            else
            {
                int outLen = length + _cipher.BlockSize;
                byte[] cipherText = new byte[outLen];
                Native.EVP_CipherUpdate(_decryptCtx, cipherText, out outLen, buf, length);
                byte[] result = new byte[outLen];
                Buffer.BlockCopy(cipherText, 0, result, 0, outLen);
                return result;
            }
        }

        private static readonly Dictionary<string, byte[]> CachedKeys = new Dictionary<string, byte[]>();
        private static readonly Dictionary<string, Cipher> CachedCiphers = new Dictionary<string, Cipher>();
        private byte[] _key;
        private Cipher _cipher;
        private IntPtr _encryptCtx;
        private IntPtr _decryptCtx;

        private void InitKey(string method, string password)
        {
            string k = method + ":" + password;
            if (CachedKeys.ContainsKey(k))
            {
                _key = CachedKeys[k];
                _cipher = CachedCiphers[k];
                return;
            }
            _cipher = Cipher.CreateByName(method);
            if (_cipher == null)
            {
                throw new NullReferenceException();
            }
            byte[] passbuf = Encoding.UTF8.GetBytes(password);
            _key = new byte[_cipher.KeyLength];
            byte[] iv = new byte[_cipher.IVLength];
            Native.EVP_BytesToKey(_cipher.Handle, MessageDigest.MD5.Handle, null, passbuf, passbuf.Length, 1, _key, iv);
            CachedKeys[k] = _key;
            CachedCiphers[k] = _cipher;
        }

        private void InitCipher(ref IntPtr ctx, byte[] iv, bool isCipher)
        {
            ctx = Native.OPENSSL_malloc(Marshal.SizeOf(typeof(CipherContext.EVP_CIPHER_CTX)));
            int enc = isCipher ? 1 : 0;
            Native.EVP_CIPHER_CTX_init(ctx);
            Native.ExpectSuccess(Native.EVP_CipherInit_ex(ctx, _cipher.Handle, IntPtr.Zero, null, null, enc));
            Native.ExpectSuccess(Native.EVP_CIPHER_CTX_set_key_length(ctx, _key.Length));
            Native.ExpectSuccess(Native.EVP_CIPHER_CTX_set_padding(ctx, 1));
            Native.ExpectSuccess(Native.EVP_CipherInit_ex(ctx, _cipher.Handle, IntPtr.Zero, _key, iv, enc));
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
                    Native.EVP_CIPHER_CTX_cleanup(_encryptCtx);
                    Native.OPENSSL_free(_encryptCtx);
                    _encryptCtx = IntPtr.Zero;
                }
                if (_decryptCtx != IntPtr.Zero)
                {
                    Native.EVP_CIPHER_CTX_cleanup(_decryptCtx);
                    Native.OPENSSL_free(_decryptCtx);
                    _decryptCtx = IntPtr.Zero;
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
