using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Shadowsocks.Encryption
{
    public class PolarSSLEncryptor
        : IVEncryptor, IDisposable
    {
        const int CIPHER_AES = 1;
        const int CIPHER_RC4 = 2;

        private IntPtr _encryptCtx = IntPtr.Zero;
        private IntPtr _decryptCtx = IntPtr.Zero;

        public PolarSSLEncryptor(string method, string password, bool onetimeauth, bool isudp)
            : base(method, password, onetimeauth, isudp)
        {
            InitKey(method, password);
        }

        private static Dictionary<string, int[]> _ciphers = new Dictionary<string, int[]> {
                {"aes-128-cfb", new int[]{16, 16, CIPHER_AES, PolarSSL.AES_CTX_SIZE}},
                {"aes-192-cfb", new int[]{24, 16, CIPHER_AES, PolarSSL.AES_CTX_SIZE}},
                {"aes-256-cfb", new int[]{32, 16, CIPHER_AES, PolarSSL.AES_CTX_SIZE}},
                {"rc4-md5", new int[]{16, 16, CIPHER_RC4, PolarSSL.ARC4_CTX_SIZE}},
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected override Dictionary<string, int[]> getCiphers()
        {
            return _ciphers;
        }

        protected override void initCipher(byte[] iv, bool isCipher)
        {
            base.initCipher(iv, isCipher);

            IntPtr ctx;
            ctx = Marshal.AllocHGlobal(_cipherInfo[3]);
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
            if (_cipher == CIPHER_AES)
            {
                PolarSSL.aes_init(ctx);
                // PolarSSL takes key length by bit
                // since we'll use CFB mode, here we both do enc, not dec
                PolarSSL.aes_setkey_enc(ctx, realkey, keyLen * 8);
            }
            else if (_cipher == CIPHER_RC4)
            {
                PolarSSL.arc4_init(ctx);
                // PolarSSL RC4 takes key length by byte
                PolarSSL.arc4_setup(ctx, realkey, keyLen);
            }
        }

        protected override void cipherUpdate(bool isCipher, int length, byte[] buf, byte[] outbuf)
        {
            // C# could be multi-threaded
            if (_disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
            byte[] iv;
            int ivOffset;
            IntPtr ctx;
            if (isCipher)
            {
                iv = _encryptIV;
                ivOffset = _encryptIVOffset;
                ctx = _encryptCtx;
            }
            else
            {
                iv = _decryptIV;
                ivOffset = _decryptIVOffset;
                ctx = _decryptCtx;
            }
            switch (_cipher)
            {
                case CIPHER_AES:
                    PolarSSL.aes_crypt_cfb128(ctx, isCipher ? PolarSSL.AES_ENCRYPT : PolarSSL.AES_DECRYPT, length, ref ivOffset, iv, buf, outbuf);
                    if (isCipher)
                    {
                        _encryptIVOffset = ivOffset;
                    }
                    else
                    {
                        _decryptIVOffset = ivOffset;
                    }
                    break;
                case CIPHER_RC4:
                    PolarSSL.arc4_crypt(ctx, length, buf, outbuf);
                    break;
            }
        }

        #region IDisposable
        private bool _disposed;

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PolarSSLEncryptor()
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
                    switch (_cipher)
                    {
                        case CIPHER_AES:
                            PolarSSL.aes_free(_encryptCtx);
                            break;
                        case CIPHER_RC4:
                            PolarSSL.arc4_free(_encryptCtx);
                            break;
                    }
                    Marshal.FreeHGlobal(_encryptCtx);
                    _encryptCtx = IntPtr.Zero;
                }
                if (_decryptCtx != IntPtr.Zero)
                {
                    switch (_cipher)
                    {
                        case CIPHER_AES:
                            PolarSSL.aes_free(_decryptCtx);
                            break;
                        case CIPHER_RC4:
                            PolarSSL.arc4_free(_decryptCtx);
                            break;
                    }
                    Marshal.FreeHGlobal(_decryptCtx);
                    _decryptCtx = IntPtr.Zero;
                }
            }
        }
        #endregion
    }
}
