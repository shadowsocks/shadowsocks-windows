using NLog;
using Shadowsocks.Controller;
using Shadowsocks.Encryption.CircularBuffer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Shadowsocks.Encryption.Stream
{
    public abstract class StreamEncryptor : EncryptorBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        // for UDP only
        protected static byte[] udpBuffer = new byte[65536];

        // every connection should create its own buffer
        private readonly ByteCircularBuffer buffer = new ByteCircularBuffer(TCPHandler.BufferSize * 2);

        // Is first packet
        protected bool ivReady;

        protected CipherFamily cipherFamily;
        protected CipherInfo CipherInfo;
        // long-time master key
        protected static byte[] key = Array.Empty<byte>();
        protected byte[] iv = Array.Empty<byte>();
        protected int keyLen;
        protected int ivLen;

        public StreamEncryptor(string method, string password)
            : base(method, password)
        {
            CipherInfo = getCiphers()[method.ToLower()];
            cipherFamily = CipherInfo.Type;
            var parameter = (StreamCipherParameter)CipherInfo.CipherParameter;
            keyLen = parameter.KeySize;
            ivLen = parameter.IvSize;

            InitKey(password);

            logger.Dump($"key {instanceId}", key, keyLen);
        }

        protected abstract Dictionary<string, CipherInfo> getCiphers();

        private void InitKey(string password)
        {
            byte[] passbuf = Encoding.UTF8.GetBytes(password);
            key ??= new byte[keyLen];
            if (key.Length != keyLen) Array.Resize(ref key, keyLen);
            LegacyDeriveKey(passbuf, key, keyLen);
        }

        public static void LegacyDeriveKey(byte[] password, byte[] key, int keylen)
        {
            byte[] result = new byte[password.Length + MD5Length];
            int i = 0;
            byte[] md5sum = Array.Empty<byte>();
            while (i < keylen)
            {
                if (i == 0)
                {
                    md5sum = CryptoUtils.MD5(password);
                }
                else
                {
                    Array.Copy(md5sum, 0, result, 0, MD5Length);
                    Array.Copy(password, 0, result, MD5Length, password.Length);
                    md5sum = CryptoUtils.MD5(result);
                }
                Array.Copy(md5sum, 0, key, i, Math.Min(MD5Length, keylen - i));
                i += MD5Length;
            }
        }

        protected virtual void initCipher(byte[] iv, bool isEncrypt)
        {
            if (ivLen == 0) return;
            this.iv = new byte[ivLen];
            Array.Copy(iv, this.iv, ivLen);
        }

        protected abstract int CipherEncrypt(Span<byte> plain, Span<byte> cipher);
        protected abstract int CipherDecrypt(Span<byte> plain, Span<byte> cipher);

        #region TCP

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            int cipherOffset = 0;
            Debug.Assert(buffer != null, "_encCircularBuffer != null");
            buffer.Put(buf, 0, length);
            logger.Trace($"{instanceId} encrypt TCP, generate iv: {!ivReady}");
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
            CipherEncrypt(plain, cipher);

            logger.DumpBase64($"plain {instanceId}", plain, size);
            logger.DumpBase64($"cipher {instanceId}", cipher, cipher.Length);
            logger.Dump($"iv {instanceId}", iv, ivLen);
            Buffer.BlockCopy(cipher, 0, outbuf, cipherOffset, size);
            outlength = size + cipherOffset;
        }

        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Debug.Assert(buffer != null, "_circularBuffer != null");
            buffer.Put(buf, 0, length);
            logger.Trace($"{instanceId} decrypt TCP, read iv: {!ivReady}");
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
            CipherDecrypt(outbuf, cipher);
            logger.DumpBase64($"cipher {instanceId}", cipher, cipher.Length);
            logger.DumpBase64($"plain {instanceId}", outbuf, cipher.Length);
            logger.Dump($"iv {instanceId}", iv, ivLen);

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
            lock (udpBuffer)
            {
                CipherEncrypt(buf, udpBuffer);
                outlength = length + ivLen;
                Buffer.BlockCopy(udpBuffer, 0, outbuf, ivLen, length);
            }
        }

        public override void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            // Get IV from first pos
            initCipher(buf, false);
            outlength = length - ivLen;
            lock (udpBuffer)
            {
                // C# could be multi-threaded
                Buffer.BlockCopy(buf, ivLen, udpBuffer, 0, length - ivLen);
                CipherDecrypt(outbuf, udpBuffer);
            }
        }

        #endregion
    }
}