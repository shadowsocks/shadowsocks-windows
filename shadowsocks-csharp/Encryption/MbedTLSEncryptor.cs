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

        public MbedTLSEncryptor(string method, string password, bool onetimeauth, bool isudp)
            : base(method, password, onetimeauth, isudp)
        {
        }

        private static Dictionary<string, Dictionary<string, int[]>> _ciphers = new Dictionary<string, Dictionary<string, int[]>> {
            { "aes-128-cfb", new Dictionary<string, int[]> { { "AES-128-CFB128", new int[] { 16, 16, CIPHER_AES } } } },
            { "aes-192-cfb", new Dictionary<string, int[]> { { "AES-192-CFB128", new int[] { 24, 16, CIPHER_AES } } } },
            { "aes-256-cfb", new Dictionary<string, int[]> { { "AES-256-CFB128", new int[] { 32, 16, CIPHER_AES } } } },
            { "aes-128-ctr", new Dictionary<string, int[]> { { "AES-128-CTR", new int[] { 16, 16, CIPHER_AES } } } },
            { "aes-192-ctr", new Dictionary<string, int[]> { { "AES-192-CTR", new int[] { 24, 16, CIPHER_AES } } } },
            { "aes-256-ctr", new Dictionary<string, int[]> { { "AES-256-CTR", new int[] { 32, 16, CIPHER_AES } } } },
            { "bf-cfb", new Dictionary<string, int[]> { { "BLOWFISH-CFB64", new int[] { 16, 8, CIPHER_BLOWFISH } } } },
            { "camellia-128-cfb", new Dictionary<string, int[]> { { "CAMELLIA-128-CFB128", new int[] { 16, 16, CIPHER_CAMELLIA } } } },
            { "camellia-192-cfb", new Dictionary<string, int[]> { { "CAMELLIA-192-CFB128", new int[] { 24, 16, CIPHER_CAMELLIA } } } },
            { "camellia-256-cfb", new Dictionary<string, int[]> { { "CAMELLIA-256-CFB128", new int[] { 32, 16, CIPHER_CAMELLIA } } } },
            { "rc4-md5", new Dictionary<string, int[]> { { "ARC4-128", new int[] { 16, 16, CIPHER_RC4 } } } }
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected override Dictionary<string, Dictionary<string, int[]>> getCiphers()
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
            if (_method == "rc4-md5")
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
            if (MbedTLS.cipher_setup( ctx, MbedTLS.cipher_info_from_string( _cipherMbedName ) ) != 0 )
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
