using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Shadowsocks.Encrypt
{
    public class PolarSSLEncryptor
        : EncryptorBase, IDisposable
    {
        const int CIPHER_AES = 1;
        const int CIPHER_RC4 = 2;
        const int CIPHER_BF = 3;

        static Dictionary<string, int[]> ciphers = new Dictionary<string, int[]> {
            {"aes-128-cfb", new int[]{16, 16, CIPHER_AES, PolarSSL.AES_CTX_SIZE}},
            {"aes-192-cfb", new int[]{24, 16, CIPHER_AES, PolarSSL.AES_CTX_SIZE}},
            {"aes-256-cfb", new int[]{32, 16, CIPHER_AES, PolarSSL.AES_CTX_SIZE}},
            {"bf-cfb", new int[]{16, 8, CIPHER_BF, PolarSSL.BLOWFISH_CTX_SIZE}},
            {"rc4", new int[]{16, 0, CIPHER_RC4, PolarSSL.ARC4_CTX_SIZE}},
            {"rc4-md5", new int[]{16, 16, CIPHER_RC4, PolarSSL.ARC4_CTX_SIZE}},
        };

        private static readonly Dictionary<string, byte[]> CachedKeys = new Dictionary<string, byte[]>();
        private int _cipher;
        private int[] _cipherInfo;
        private byte[] _key;
        private IntPtr _encryptCtx = IntPtr.Zero;
        private IntPtr _decryptCtx = IntPtr.Zero;
        private byte[] _encryptIV;
        private byte[] _decryptIV;
        private byte[] _encryptIVOffset;
        private byte[] _decryptIVOffset;
        private string _method;
        private int keyLen;
        private int ivLen;

        public PolarSSLEncryptor(string method, string password)
            : base(method, password)
        {
            InitKey(method, password);
        }

        private static void randBytes(byte[] buf, int length)
        {
            byte[] temp = new byte[length];
            new Random().NextBytes(temp);
            temp.CopyTo(buf, 0);
        }

        private void bytesToKey(byte[] password, byte[] key)
        {
            byte[] result = new byte[password.Length + 16];
            int i = 0;
            byte[] md5sum = null;
            while (i < key.Length)
            {
                MD5 md5 = MD5.Create();
                if (i == 0)
                {
                    md5sum = md5.ComputeHash(password);
                }
                else
                {
                    md5sum.CopyTo(result, 0);
                    password.CopyTo(result, md5sum.Length);
                    md5sum = md5.ComputeHash(result);
                }
                md5sum.CopyTo(key, i);
                i += md5sum.Length;
            }
        }

        private void InitKey(string method, string password)
        {
            method = method.ToLower();
            _method = method;
            string k = method + ":" + password;
            _cipherInfo = ciphers[_method];
            _cipher = _cipherInfo[2];
            if (_cipher == 0)
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
                bytesToKey(passbuf, _key);
                CachedKeys[k] = _key;
            }
        }

        private void InitCipher(ref IntPtr ctx, byte[] iv, bool isCipher)
        {
            ctx = Marshal.AllocHGlobal(_cipherInfo[3]);
            byte[] realkey;
            if (_method == "rc4-md5")
            {
                byte[] temp = new byte[keyLen + ivLen];
                realkey = new byte[keyLen];
                Array.Copy(_key, 0, temp, 0, keyLen);
                Array.Copy(iv, 0, temp, keyLen, ivLen);
                realkey = MD5.Create().ComputeHash(temp);
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
                if (isCipher)
                {
                    _encryptIV = new byte[ivLen];
                    _encryptIVOffset = new byte[8];
                    Array.Copy(iv, _encryptIV, ivLen);
                }
                else
                {
                    _decryptIV = new byte[ivLen];
                    _decryptIVOffset = new byte[8];
                    Array.Copy(iv, _decryptIV, ivLen);
                }
            }
            else if (_cipher == CIPHER_BF)
            {
                PolarSSL.blowfish_init(ctx);
                // PolarSSL takes key length by bit
                PolarSSL.blowfish_setkey(ctx, realkey, keyLen * 8);
                if (isCipher)
                {
                    _encryptIV = new byte[ivLen];
                    _encryptIVOffset = new byte[8];
                    Array.Copy(iv, _encryptIV, ivLen);
                }
                else
                {
                    _decryptIV = new byte[ivLen];
                    _decryptIVOffset = new byte[8];
                    Array.Copy(iv, _decryptIV, ivLen);
                }
            }
            else if (_cipher == CIPHER_RC4)
            {
                PolarSSL.arc4_init(ctx);
                // PolarSSL RC4 takes key length by byte
                PolarSSL.arc4_setup(ctx, realkey, keyLen);
            }
        }



        static byte[] tempbuf = new byte[32768];

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            if (_encryptCtx == IntPtr.Zero)
            {
                randBytes(outbuf, ivLen);
                InitCipher(ref _encryptCtx, outbuf, true);
                outlength = length + ivLen;
                lock (tempbuf)
                {
                    // C# could be multi-threaded
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(this.ToString());
                    }
                    switch (_cipher)
                    {
                        case CIPHER_AES:
                            PolarSSL.aes_crypt_cfb128(_encryptCtx, PolarSSL.AES_ENCRYPT, length, _encryptIVOffset, _encryptIV, buf, tempbuf);
                            break;
                        case CIPHER_BF:
                            PolarSSL.blowfish_crypt_cfb64(_encryptCtx, PolarSSL.BLOWFISH_ENCRYPT, length, _encryptIVOffset, _encryptIV, buf, tempbuf);
                            break;
                        case CIPHER_RC4:
                            PolarSSL.arc4_crypt(_encryptCtx, length, buf, tempbuf);
                            break;
                    }
                    outlength = length + ivLen;
                    Buffer.BlockCopy(tempbuf, 0, outbuf, ivLen, length);

                }
            }
            else
            {
                outlength = length;
                if (_disposed)
                {
                    throw new ObjectDisposedException(this.ToString());
                }
                switch (_cipher)
                {
                    case CIPHER_AES:
                        PolarSSL.aes_crypt_cfb128(_encryptCtx, PolarSSL.AES_ENCRYPT, length, _encryptIVOffset, _encryptIV, buf, outbuf);
                        break;
                    case CIPHER_BF:
                        PolarSSL.blowfish_crypt_cfb64(_encryptCtx, PolarSSL.BLOWFISH_ENCRYPT, length, _encryptIVOffset, _encryptIV, buf, outbuf);
                        break;
                    case CIPHER_RC4:
                        PolarSSL.arc4_crypt(_encryptCtx, length, buf, outbuf);
                        break;
                }
            }
        }

        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            if (_decryptCtx == IntPtr.Zero)
            {
                InitCipher(ref _decryptCtx, buf, false);
                outlength = length - ivLen;
                lock (tempbuf)
                {
                    // C# could be multi-threaded
                    Buffer.BlockCopy(buf, ivLen, tempbuf, 0, length - ivLen);
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(this.ToString());
                    }
                    switch (_cipher)
                    {
                        case CIPHER_AES:
                            PolarSSL.aes_crypt_cfb128(_decryptCtx, PolarSSL.AES_DECRYPT, length - ivLen, _decryptIVOffset, _decryptIV, tempbuf, outbuf);
                            break;
                        case CIPHER_BF:
                            PolarSSL.blowfish_crypt_cfb64(_decryptCtx, PolarSSL.BLOWFISH_DECRYPT, length - ivLen, _decryptIVOffset, _decryptIV, tempbuf, outbuf);
                            break;
                        case CIPHER_RC4:
                            PolarSSL.arc4_crypt(_decryptCtx, length - ivLen, tempbuf, outbuf);
                            break;
                    }
                }
            }
            else
            {
                outlength = length;
                if (_disposed)
                {
                    throw new ObjectDisposedException(this.ToString());
                }
                switch (_cipher)
                {
                    case CIPHER_AES:
                        PolarSSL.aes_crypt_cfb128(_decryptCtx, PolarSSL.AES_DECRYPT, length, _decryptIVOffset, _decryptIV, buf, outbuf);
                        break;
                    case CIPHER_BF:
                        PolarSSL.blowfish_crypt_cfb64(_decryptCtx, PolarSSL.BLOWFISH_DECRYPT, length, _decryptIVOffset, _decryptIV, buf, outbuf);
                        break;
                    case CIPHER_RC4:
                        PolarSSL.arc4_crypt(_decryptCtx, length, buf, outbuf);
                        break;
                }
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
                        case CIPHER_BF:
                            PolarSSL.blowfish_free(_encryptCtx);
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
                        case CIPHER_BF:
                            PolarSSL.blowfish_free(_decryptCtx);
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
