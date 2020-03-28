using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shadowsocks.Encryption.Stream
{
    public abstract class StreamEncryptor : EncryptorBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // shared by TCP decrypt UDP encrypt and decrypt
        protected static byte[] sharedBuffer = new byte[65536];

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
            StreamCipherParameter parameter = (StreamCipherParameter)CipherInfo.CipherParameter;
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
            if (key.Length != keyLen)
            {
                Array.Resize(ref key, keyLen);
            }

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
            if (ivLen == 0)
            {
                return;
            }

            this.iv = new byte[ivLen];
            Array.Copy(iv, this.iv, ivLen);
        }

        protected abstract int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher);
        protected abstract int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher);

        #region TCP
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            int cipherOffset = 0;
            Span<byte> tmp = buf.AsSpan(0, length);
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
            int size = tmp.Length;

            byte[] cipher = new byte[size];
            CipherEncrypt(tmp, cipher);

            logger.DumpBase64($"plain {instanceId}", tmp.ToArray(), size);
            logger.DumpBase64($"cipher {instanceId}", cipher, cipher.Length);
            logger.Dump($"iv {instanceId}", iv, ivLen);
            Buffer.BlockCopy(cipher, 0, outbuf, cipherOffset, size);
            outlength = size + cipherOffset;
        }

        private int recieveCtr = 0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Span<byte> tmp = buf.AsSpan(0, length);
            logger.Trace($"{instanceId} decrypt TCP, read iv: {!ivReady}");

            // is first packet, need read iv
            if (!ivReady)
            {
                // push to buffer in case of not enough data
                tmp.CopyTo(sharedBuffer.AsSpan(recieveCtr));
                recieveCtr += tmp.Length;

                // not enough data for read iv, return 0 byte data
                if (recieveCtr <= ivLen)
                {
                    outlength = 0;
                    return;
                }
                // start decryption
                ivReady = true;
                if (ivLen > 0)
                {
                    // read iv
                    byte[] iv = sharedBuffer.AsSpan(0, ivLen).ToArray();
                    initCipher(iv, false);
                }
                else
                {
                    initCipher(Array.Empty<byte>(), false);
                }

                tmp = sharedBuffer.AsSpan(ivLen, recieveCtr - ivLen);
            }

            // read all data from buffer
            CipherDecrypt(outbuf, tmp);
            logger.DumpBase64($"cipher {instanceId}", tmp.ToArray(), tmp.Length);
            logger.DumpBase64($"plain {instanceId}", outbuf, tmp.Length);
            logger.Dump($"iv {instanceId}", iv, ivLen);
            outlength = tmp.Length;
        }

        #endregion

        #region UDP
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void EncryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            // Generate IV
            RNG.GetBytes(outbuf, ivLen);
            initCipher(outbuf, true);
            lock (sharedBuffer)
            {
                CipherEncrypt(buf, sharedBuffer);
                outlength = length + ivLen;
                Buffer.BlockCopy(sharedBuffer, 0, outbuf, ivLen, length);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            // Get IV from first pos
            initCipher(buf, false);
            outlength = length - ivLen;
            lock (sharedBuffer)
            {
                // C# could be multi-threaded
                Buffer.BlockCopy(buf, ivLen, sharedBuffer, 0, length - ivLen);
                CipherDecrypt(outbuf, sharedBuffer);
            }
        }

        #endregion

        public override int Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            throw new NotImplementedException();
        }

        public override int Decrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            throw new NotImplementedException();
        }

        public override int EncryptUDP(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            throw new NotImplementedException();
        }

        public override int DecryptUDP(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            throw new NotImplementedException();
        }
    }
}