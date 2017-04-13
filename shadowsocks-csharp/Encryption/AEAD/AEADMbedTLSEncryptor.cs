using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Shadowsocks.Encryption.Exception;

namespace Shadowsocks.Encryption.AEAD
{
    public class AEADMbedTLSEncryptor
        : AEADEncryptor, IDisposable
    {
        const int CIPHER_AES = 1;

        private IntPtr _encryptCtx = IntPtr.Zero;
        private IntPtr _decryptCtx = IntPtr.Zero;

        public AEADMbedTLSEncryptor(string method, string password)
            : base(method, password)
        {
        }

        private static Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>
        {
            {"aes-128-gcm", new EncryptorInfo("AES-128-GCM", 16, 16, 12, 16, CIPHER_AES)},
            {"aes-192-gcm", new EncryptorInfo("AES-192-GCM", 24, 24, 12, 16, CIPHER_AES)},
            {"aes-256-gcm", new EncryptorInfo("AES-256-GCM", 32, 32, 12, 16, CIPHER_AES)},
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        public override void InitCipher(byte[] salt, bool isEncrypt, bool isUdp)
        {
            base.InitCipher(salt, isEncrypt, isUdp);
            IntPtr ctx = Marshal.AllocHGlobal(MbedTLS.cipher_get_size_ex());
            if (isEncrypt)
            {
                _encryptCtx = ctx;
            }
            else
            {
                _decryptCtx = ctx;
            }
            MbedTLS.cipher_init(ctx);
            if (MbedTLS.cipher_setup(ctx, MbedTLS.cipher_info_from_string(_innerLibName)) != 0)
                throw new System.Exception("Cannot initialize mbed TLS cipher context");

            DeriveSessionKey(isEncrypt ? _encryptSalt : _decryptSalt,
                _Masterkey, _sessionKey);
            CipherSetKey(isEncrypt, _sessionKey);
        }

        private void CipherSetKey(bool isEncrypt, byte[] key)
        {
            IntPtr ctx = isEncrypt ? _encryptCtx : _decryptCtx;
            int ret = MbedTLS.cipher_setkey(ctx, key, keyLen * 8,
                isEncrypt ? MbedTLS.MBEDTLS_ENCRYPT : MbedTLS.MBEDTLS_DECRYPT);
            if (ret != 0) throw new System.Exception("failed to set key");
            ret = MbedTLS.cipher_reset(ctx);
            if (ret != 0) throw new System.Exception("failed to finish preparation");
        }

        public override int cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            // buf: all plaintext
            // outbuf: ciphertext + tag
            int ret;
            byte[] tagbuf = new byte[tagLen];
            uint olen = 0;
            switch (_cipher)
            {
                case CIPHER_AES:
                    ret = MbedTLS.cipher_auth_encrypt(_encryptCtx,
                        /* nonce */
                        _encNonce, (uint) nonceLen,
                        /* AD */
                        IntPtr.Zero, 0,
                        /* plain */
                        plaintext, plen,
                        /* cipher */
                        ciphertext, ref olen,
                        tagbuf, (uint) tagLen);
                    if (ret != 0) throw new CryptoErrorException();
                    Debug.Assert(olen == plen);
                    // attach tag to ciphertext
                    Array.Copy(tagbuf, 0, ciphertext, (int) plen, tagLen);
                    clen = olen + (uint) tagLen;
                    return ret;
                default:
                    throw new System.Exception("not implemented");
            }
        }

        public override int cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            // buf: ciphertext + tag
            // outbuf: plaintext
            int ret;
            uint olen = 0;
            // split tag
            byte[] tagbuf = new byte[tagLen];
            Array.Copy(ciphertext, (int) (clen - tagLen), tagbuf, 0, tagLen);
            switch (_cipher)
            {
                case CIPHER_AES:
                    ret = MbedTLS.cipher_auth_decrypt(_decryptCtx,
                        _decNonce, (uint) nonceLen,
                        IntPtr.Zero, 0,
                        ciphertext, (uint) (clen - tagLen),
                        plaintext, ref olen,
                        tagbuf, (uint) tagLen);
                    if (ret != 0) throw new CryptoErrorException();
                    Debug.Assert(olen == clen - tagLen);
                    plen = olen;
                    return ret;
                default:
                    throw new System.Exception("not implemented");
            }
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

        ~AEADMbedTLSEncryptor()
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
                MbedTLS.cipher_free(_encryptCtx);
                Marshal.FreeHGlobal(_encryptCtx);
                _encryptCtx = IntPtr.Zero;
            }
            if (_decryptCtx != IntPtr.Zero)
            {
                MbedTLS.cipher_free(_decryptCtx);
                Marshal.FreeHGlobal(_decryptCtx);
                _decryptCtx = IntPtr.Zero;
            }
        }

        #endregion
    }
}