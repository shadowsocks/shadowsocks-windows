using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Shadowsocks.Encryption
{
    public class MbedTLSEncryptor
        : IVEncryptor, IDisposable
    {
        const int CIPHER_RC4 = 1;
        const int CIPHER_AES = 2;
        const int CIPHER_BLOWFISH = 3;
        const int CIPHER_CAMELLIA = 4;

        private IntPtr _encryptCtx = IntPtr.Zero;
        private IntPtr _decryptCtx = IntPtr.Zero;

        public MbedTLSEncryptor(string method, string password)
            : base(method, password)
        {
        }

        private static Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo> {
            { "aes-128-cfb", new EncryptorInfo(16, 16, true, CIPHER_AES, 0, "AES-128-CFB128") },
            { "aes-192-cfb", new EncryptorInfo(24, 16, true, CIPHER_AES, 0, "AES-192-CFB128") },
            { "aes-256-cfb", new EncryptorInfo(32, 16, true, CIPHER_AES, 0, "AES-256-CFB128") },
            { "aes-128-ctr", new EncryptorInfo(16, 16, true, CIPHER_AES, 0, "AES-128-CTR") },
            { "aes-192-ctr", new EncryptorInfo(24, 16, true, CIPHER_AES, 0, "AES-192-CTR") },
            { "aes-256-ctr", new EncryptorInfo(32, 16, true, CIPHER_AES, 0, "AES-256-CTR") },
            { "bf-cfb", new EncryptorInfo(16, 8, true, CIPHER_BLOWFISH, 0, "BLOWFISH-CFB64") },
            { "camellia-128-cfb", new EncryptorInfo(16, 16, true, CIPHER_CAMELLIA, 0, "CAMELLIA-128-CFB128") },
            { "camellia-192-cfb", new EncryptorInfo(24, 16, true, CIPHER_CAMELLIA, 0, "CAMELLIA-192-CFB128") },
            { "camellia-256-cfb", new EncryptorInfo(32, 16, true, CIPHER_CAMELLIA, 0, "CAMELLIA-256-CFB128") },
            { "rc4", new EncryptorInfo(16, 16, false, CIPHER_RC4, 0, "ARC4-128") },
            { "rc4-md5", new EncryptorInfo(16, 16, true, CIPHER_RC4, 0, "ARC4-128") },
            { "rc4-md5-6", new EncryptorInfo(16, 6, true, CIPHER_RC4, 0, "ARC4-128") },
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        protected override void initCipher(byte[] iv, bool isCipher)
        {
            base.initCipher(iv, isCipher);
            IntPtr ctx = Marshal.AllocHGlobal(MbedTLS.cipher_get_size_ex());
            if (isCipher)
            {
                _encryptCtx = ctx;
            }
            else
            {
                _decryptCtx = ctx;
            }
            byte[] realkey;
            if (_method.StartsWith("rc4-"))
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
            MbedTLS.cipher_init(ctx);
            if (MbedTLS.cipher_setup( ctx, MbedTLS.cipher_info_from_string( getInfo().name ) ) != 0 )
                throw new Exception("Cannot initialize mbed TLS cipher context");
            /*
             * MbedTLS takes key length by bit
             * cipher_setkey() will set the correct key schedule
             * and operation
             *
             *  MBEDTLS_AES_{EN,DE}CRYPT
             *  == MBEDTLS_BLOWFISH_{EN,DE}CRYPT
             *  == MBEDTLS_CAMELLIA_{EN,DE}CRYPT
             *  == MBEDTLS_{EN,DE}CRYPT
             *  
             */
            if (MbedTLS.cipher_setkey(ctx, realkey, keyLen * 8,
                isCipher ? MbedTLS.MBEDTLS_ENCRYPT : MbedTLS.MBEDTLS_DECRYPT) != 0 )
                throw new Exception("Cannot set mbed TLS cipher key");
            if (MbedTLS.cipher_set_iv(ctx, iv, ivLen) != 0)
                throw new Exception("Cannot set mbed TLS cipher IV");
            if (MbedTLS.cipher_reset(ctx) != 0)
                throw new Exception("Cannot finalize mbed TLS cipher context");
        }

        protected override void cipherUpdate(bool isCipher, int length, byte[] buf, byte[] outbuf)
        {
            // C# could be multi-threaded
            if (_disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
            if (MbedTLS.cipher_update(isCipher ? _encryptCtx : _decryptCtx,
                buf, length, outbuf, ref length) != 0 )
                throw new Exception("Cannot update mbed TLS cipher context");
        }

        #region IDisposable
        private bool _disposed;

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MbedTLSEncryptor()
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
        }
        #endregion
    }
}
