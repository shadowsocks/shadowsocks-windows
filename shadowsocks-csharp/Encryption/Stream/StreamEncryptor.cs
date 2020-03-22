using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Shadowsocks.Encryption.CircularBuffer;
using Shadowsocks.Controller;

namespace Shadowsocks.Encryption.Stream
{
    public abstract class StreamEncryptor : EncryptorBase
    {
        // for UDP only
        protected static byte[] _udpTmpBuf = new byte[65536];

        // every connection should create its own buffer
        private ByteCircularBuffer buffer = new ByteCircularBuffer(TCPHandler.BufferSize * 2);

        protected Dictionary<string, CipherInfo> ciphers;


        // Is first packet
        protected bool ivReady;

        protected string _method;
        protected CipherFamily _cipher;
        protected CipherInfo CipherInfo;
        // long-time master key
        protected static byte[] key = null;
        protected byte[] iv;
        protected int keyLen;
        protected int ivLen;

        public StreamEncryptor(string method, string password)
            : base(method, password)
        {
            InitEncryptorInfo(method);
            InitKey(password);
        }

        protected abstract Dictionary<string, CipherInfo> getCiphers();

        private void InitEncryptorInfo(string method)
        {
            method = method.ToLower();
            _method = method;
            ciphers = getCiphers();
            CipherInfo = ciphers[_method];
            _cipher = CipherInfo.Type;
            var parameter = (StreamCipherParameter)CipherInfo.CipherParameter;
            keyLen = parameter.KeySize;
            ivLen = parameter.IvSize;
        }

        private void InitKey(string password)
        {
            byte[] passbuf = Encoding.UTF8.GetBytes(password);
            key ??= new byte[keyLen];
            if (key.Length != keyLen) Array.Resize(ref key, keyLen);
            LegacyDeriveKey(passbuf, key, keyLen);
        }

        public static void LegacyDeriveKey(byte[] password, byte[] key, int keylen)
        {
            byte[] result = new byte[password.Length + MD5_LEN];
            int i = 0;
            byte[] md5sum = null;
            while (i < keylen)
            {
                if (i == 0)
                {
                    md5sum = CryptoUtils.MD5(password);
                }
                else
                {
                    Array.Copy(md5sum, 0, result, 0, MD5_LEN);
                    Array.Copy(password, 0, result, MD5_LEN, password.Length);
                    md5sum = CryptoUtils.MD5(result);
                }
                Array.Copy(md5sum, 0, key, i, Math.Min(MD5_LEN, keylen - i));
                i += MD5_LEN;
            }
        }

        protected virtual void initCipher(byte[] iv, bool isEncrypt)
        {
            this.iv = new byte[ivLen];
            Array.Copy(iv, this.iv, ivLen);
        }

        protected abstract void cipherUpdate(bool isEncrypt, int length, byte[] buf, byte[] outbuf);

        protected abstract int CipherEncrypt(Span<byte> plain, Span<byte> cipher);
        protected abstract int CipherDecrypt(Span<byte> plain, Span<byte> cipher);

        //protected static void randBytes(byte[] buf, int length) { RNG.GetBytes(buf, length); }

        #region TCP

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            int cipherOffset = 0;
            Debug.Assert(buffer != null, "_encCircularBuffer != null");
            buffer.Put(buf, 0, length);
            if (!ivReady)
            {
                // Generate IV
                byte[] ivBytes = RNG.GetBytes(ivLen);
                initCipher(ivBytes, true);

                Array.Copy(ivBytes, 0, outbuf, 0, ivLen);
                cipherOffset = ivLen;
                ivReady = true;
            }
            int size = buffer.Size;
            byte[] plain = buffer.Get(size);
            byte[] cipher = new byte[size];
            cipherUpdate(true, size, plain, cipher);
            Buffer.BlockCopy(cipher, 0, outbuf, cipherOffset, size);
            outlength = size + cipherOffset;
        }

        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Debug.Assert(buffer != null, "_circularBuffer != null");
            buffer.Put(buf, 0, length);
            if (!ivReady)
            {
                if (buffer.Size <= ivLen)
                {
                    // we need more data
                    outlength = 0;
                    return;
                }
                // start decryption
                ivReady = true;
                if (ivLen > 0)
                {
                    byte[] iv = buffer.Get(ivLen);
                    initCipher(iv, false);
                }
                else initCipher(Array.Empty<byte>(), false);
            }
            byte[] cipher = buffer.ToArray();
            cipherUpdate(false, cipher.Length, cipher, outbuf);
            // move pointer only
            buffer.Skip(buffer.Size);
            outlength = cipher.Length;
            // done the decryption
        }

        #endregion

        #region UDP

        public override void EncryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            // Generate IV
            RNG.GetBytes(outbuf, ivLen);
            initCipher(outbuf, true);
            lock (_udpTmpBuf)
            {
                cipherUpdate(true, length, buf, _udpTmpBuf);
                outlength = length + ivLen;
                Buffer.BlockCopy(_udpTmpBuf, 0, outbuf, ivLen, length);
            }
        }

        public override void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            // Get IV from first pos
            initCipher(buf, false);
            outlength = length - ivLen;
            lock (_udpTmpBuf)
            {
                // C# could be multi-threaded
                Buffer.BlockCopy(buf, ivLen, _udpTmpBuf, 0, length - ivLen);
                cipherUpdate(false, length - ivLen, _udpTmpBuf, outbuf);
            }
        }

        #endregion
    }
}