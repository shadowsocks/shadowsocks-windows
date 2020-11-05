using Splat;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shadowsocks.Net.Crypto.Stream
{
    public abstract class StreamCrypto : CryptoBase, IEnableLogger
    {
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

        public StreamCrypto(string method, string password)
            : base(method, password)
        {
            CipherInfo = GetCiphers()[method.ToLower()];
            cipherFamily = CipherInfo.Type;
            StreamCipherParameter parameter = (StreamCipherParameter)CipherInfo.CipherParameter;
            keyLen = parameter.KeySize;
            ivLen = parameter.IvSize;

            InitKey(password);

            this.Log().Debug($"key {instanceId} {key} {keyLen}");
        }

        protected abstract Dictionary<string, CipherInfo> GetCiphers();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        protected virtual void InitCipher(byte[] iv, bool isEncrypt)
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
        public override int Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            int cipherOffset = 0;
            this.Log().Debug($"{instanceId} encrypt TCP, generate iv: {!ivReady}");
            if (!ivReady)
            {
                // Generate IV
                byte[] ivBytes = RNG.GetBytes(ivLen);
                InitCipher(ivBytes, true);
                ivBytes.CopyTo(cipher);
                cipherOffset = ivLen;
                cipher = cipher.Slice(cipherOffset);
                ivReady = true;
            }
            int clen = CipherEncrypt(plain, cipher);

            this.Log().Debug($"plain {instanceId} {Convert.ToBase64String(plain)}");
            this.Log().Debug($"cipher {instanceId} {Convert.ToBase64String(cipher.Slice(0, clen))}");
            this.Log().Debug($"iv {instanceId} {iv} {ivLen}");
            return clen + cipherOffset;
        }

        private int recieveCtr = 0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int Decrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            this.Log().Debug($"{instanceId} decrypt TCP, read iv: {!ivReady}");

            int cipherOffset = 0;
            // is first packet, need read iv
            if (!ivReady)
            {
                // push to buffer in case of not enough data
                cipher.CopyTo(sharedBuffer.AsSpan(recieveCtr));
                recieveCtr += cipher.Length;

                // not enough data for read iv, return 0 byte data
                if (recieveCtr <= ivLen)
                {
                    return 0;
                }
                // start decryption
                ivReady = true;
                if (ivLen > 0)
                {
                    // read iv
                    byte[] iv = sharedBuffer.AsSpan(0, ivLen).ToArray();
                    InitCipher(iv, false);
                }
                else
                {
                    InitCipher(Array.Empty<byte>(), false);
                }
                cipherOffset += ivLen;
            }

            // read all data from buffer
            int len = CipherDecrypt(plain, cipher.Slice(cipherOffset));

            this.Log().Debug($"cipher {instanceId} {Convert.ToBase64String(cipher.Slice(cipherOffset))}");
            this.Log().Debug($"plain {instanceId} {Convert.ToBase64String(plain.Slice(0, len))}");
            this.Log().Debug($"iv {instanceId} {iv} {ivLen}");
            return len;
        }

        #endregion

        #region UDP
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int EncryptUDP(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            byte[] iv = RNG.GetBytes(ivLen);
            iv.CopyTo(cipher);
            InitCipher(iv, true);
            return ivLen + CipherEncrypt(plain, cipher.Slice(ivLen));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int DecryptUDP(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            InitCipher(cipher.Slice(0, ivLen).ToArray(), false);
            return CipherDecrypt(plain, cipher.Slice(ivLen));
        }

        #endregion
    }
}
