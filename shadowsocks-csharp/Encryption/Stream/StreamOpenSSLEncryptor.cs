using System;
using System.Collections.Generic;
using System.Diagnostics;
using Shadowsocks.Encryption.Exception;


namespace Shadowsocks.Encryption.Stream
{
    public class StreamOpenSSLEncryptor
        : StreamEncryptor, IDisposable
    {
        const int CIPHER_RC4 = 1;
        const int CIPHER_AES = 2;
        const int CIPHER_CAMELLIA = 3;
        const int CIPHER_BLOWFISH = 4;
        const int CIPHER_CHACHA20_IETF = 5;

        private IntPtr _encryptCtx = IntPtr.Zero;
        private IntPtr _decryptCtx = IntPtr.Zero;

        public StreamOpenSSLEncryptor(string method, string password)
            : base(method, password)
        {
        }

        // XXX: name=RC4,blkSz=1,keyLen=16,ivLen=0, do NOT pass IV to it
        private static readonly Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>
        {
            { "aes-128-cfb", new EncryptorInfo("AES-128-CFB", 16, 16, CIPHER_AES) },
            { "aes-192-cfb", new EncryptorInfo("AES-192-CFB", 24, 16, CIPHER_AES) },
            { "aes-256-cfb", new EncryptorInfo("AES-256-CFB", 32, 16, CIPHER_AES) },
            { "aes-128-ctr", new EncryptorInfo("aes-128-ctr", 16, 16, CIPHER_AES) },
            { "aes-192-ctr", new EncryptorInfo("aes-192-ctr", 24, 16, CIPHER_AES) },
            { "aes-256-ctr", new EncryptorInfo("aes-256-ctr", 32, 16, CIPHER_AES) },
            { "bf-cfb", new EncryptorInfo("bf-cfb", 16, 8, CIPHER_BLOWFISH) },
            { "camellia-128-cfb", new EncryptorInfo("CAMELLIA-128-CFB", 16, 16, CIPHER_CAMELLIA) },
            { "camellia-192-cfb", new EncryptorInfo("CAMELLIA-192-CFB", 24, 16, CIPHER_CAMELLIA) },
            { "camellia-256-cfb", new EncryptorInfo("CAMELLIA-256-CFB", 32, 16, CIPHER_CAMELLIA) },
            { "rc4-md5", new EncryptorInfo("RC4", 16, 16, CIPHER_RC4) },
            // it's using ivLen=16, not compatible
            //{ "chacha20-ietf", new EncryptorInfo("chacha20", 32, 12, CIPHER_CHACHA20_IETF) }
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        protected override void initCipher(byte[] iv, bool isEncrypt)
        {
            base.initCipher(iv, isEncrypt);
            IntPtr cipherInfo = OpenSSL.GetCipherInfo(_innerLibName);
            if (cipherInfo == IntPtr.Zero) throw new System.Exception("openssl: cipher not found");
            IntPtr ctx = OpenSSL.EVP_CIPHER_CTX_new();
            if (ctx == IntPtr.Zero) throw new System.Exception("fail to create ctx");
            
            if (isEncrypt)
            {
                _encryptCtx = ctx;
            }
            else
            {
                _decryptCtx = ctx;
            }

            byte[] realkey;
            if (_method == "rc4-md5")
            {
                byte[] temp = new byte[keyLen + ivLen];
                Array.Copy(_key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                realkey = MbedTLS.MD5(temp);
            }
            else
            {
                realkey = _key;
            }
            
            var ret = OpenSSL.EVP_CipherInit_ex(ctx, cipherInfo, IntPtr.Zero, null,null,
                isEncrypt ? OpenSSL.OPENSSL_ENCRYPT : OpenSSL.OPENSSL_DECRYPT);
            if (ret != 1) throw new System.Exception("openssl: fail to set key length");
            ret = OpenSSL.EVP_CIPHER_CTX_set_key_length(ctx, keyLen);
            if (ret != 1) throw new System.Exception("openssl: fail to set key length");
            ret = OpenSSL.EVP_CipherInit_ex(ctx, IntPtr.Zero, IntPtr.Zero, realkey,
                _method == "rc4-md5" ? null : iv,
                isEncrypt ? OpenSSL.OPENSSL_ENCRYPT : OpenSSL.OPENSSL_DECRYPT);
            if (ret != 1) throw new System.Exception("openssl: cannot set key and iv");
            OpenSSL.EVP_CIPHER_CTX_set_padding(ctx, 0);
        }

        protected override void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf)
        {
            // C# could be multi-threaded
            if (_disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            int outlen = 0;
            var ret = OpenSSL.EVP_CipherUpdate(isEncrypt ? _encryptCtx : _decryptCtx,
                outbuf, out outlen, buf, length);
            if (ret != 1)
                throw new CryptoErrorException(String.Format("ret is {0}", ret));
            Debug.Assert(outlen == length);
        }

        #region IDisposable

        private bool _disposed;

        // instance based lock
        private readonly object _lock = new object();

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~StreamOpenSSLEncryptor()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            if (disposing)
            {
                // free managed objects
            }

            // free unmanaged objects
            if (_encryptCtx != IntPtr.Zero)
            {
                OpenSSL.EVP_CIPHER_CTX_free(_encryptCtx);
                _encryptCtx = IntPtr.Zero;
            }

            if (_decryptCtx != IntPtr.Zero)
            {
                OpenSSL.EVP_CIPHER_CTX_free(_decryptCtx);
                _decryptCtx = IntPtr.Zero;
            }
        }

        #endregion
    }
}