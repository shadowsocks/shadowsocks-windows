using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using Shadowsocks.Encryption.CircularBuffer;
using Shadowsocks.Controller;
using Shadowsocks.Encryption.Exception;
using Shadowsocks.Encryption.Stream;

namespace Shadowsocks.Encryption.AEAD
{
    public abstract class AEADEncryptor : EncryptorBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        // We are using the same saltLen and keyLen
        private const string Info = "ss-subkey";
        private static readonly byte[] InfoBytes = Encoding.ASCII.GetBytes(Info);

        // every connection should create its own buffer
        private ByteCircularBuffer buffer = new ByteCircularBuffer(MaxInputSize * 2);

        public const int ChunkLengthBytes = 2;
        public const uint ChunkLengthMask = 0x3FFFu;

        protected Dictionary<string, CipherInfo> ciphers;

        protected CipherFamily cipherFamily;
        protected CipherInfo CipherInfo;
        protected static byte[] masterKey = Array.Empty<byte>();
        protected byte[] sessionKey = Array.Empty<byte>();
        protected int keyLen;
        protected int saltLen;
        protected int tagLen;
        protected int nonceLen;

        protected byte[] salt;
        protected byte[] nonce;

        // Is first packet
        protected bool saltReady;

        // Is first chunk(tcp request)
        protected bool tcpRequestSent;

        public AEADEncryptor(string method, string password)
            : base(method, password)
        {
            CipherInfo = getCiphers()[method.ToLower()];
            cipherFamily = CipherInfo.Type;
            var parameter = (AEADCipherParameter)CipherInfo.CipherParameter;
            keyLen = parameter.KeySize;
            saltLen = parameter.SaltSize;
            tagLen = parameter.TagSize;
            nonceLen = parameter.NonceSize;

            InitKey(password);
            // Initialize all-zero nonce for each connection
            nonce = new byte[nonceLen];
        }

        protected abstract Dictionary<string, CipherInfo> getCiphers();

        protected void InitKey(string password)
        {
            byte[] passbuf = Encoding.UTF8.GetBytes(password);
            // init master key
            if (masterKey == null) masterKey = new byte[keyLen];
            if (masterKey.Length != keyLen) Array.Resize(ref masterKey, keyLen);
            DeriveKey(passbuf, masterKey, keyLen);
            // init session key
            sessionKey = new byte[keyLen];
        }

        public void DeriveKey(byte[] password, byte[] key, int keylen)
        {
            StreamEncryptor.LegacyDeriveKey(password, key, keylen);
        }

        public void DeriveSessionKey(byte[] salt, byte[] masterKey, byte[] sessionKey)
        {
            CryptoUtils.HKDF(keyLen, masterKey, salt, InfoBytes).CopyTo(sessionKey, 0);
        }

        protected void IncrementNonce()
        {
            CryptoUtils.SodiumIncrement(nonce);
        }

        public virtual void InitCipher(byte[] salt, bool isEncrypt, bool isUdp)
        {
            this.salt = new byte[saltLen];
            Array.Copy(salt, this.salt, saltLen);
            logger.Dump("Salt", salt, saltLen);

            DeriveSessionKey(salt, masterKey, sessionKey);
        }

        public abstract int CipherEncrypt(Span<byte> plain, Span<byte> cipher);
        public abstract int CipherDecrypt(Span<byte> plain, Span<byte> cipher);

        #region TCP

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Debug.Assert(buffer != null, "_encCircularBuffer != null");

            buffer.Put(buf, 0, length);
            outlength = 0;
            logger.Debug("---Start Encryption");
            if (!saltReady)
            {
                saltReady = true;
                // Generate salt
                byte[] saltBytes = RNG.GetBytes(saltLen);
                InitCipher(saltBytes, true, false);
                Array.Copy(saltBytes, 0, outbuf, 0, saltLen);
                outlength = saltLen;
                logger.Debug($"_encryptSaltSent outlength {outlength}");
            }

            if (!tcpRequestSent)
            {
                tcpRequestSent = true;
                // The first TCP request
                byte[] encAddrBufBytes = new byte[AddressBufferLength + tagLen * 2 + ChunkLengthBytes];
                byte[] addrBytes = buffer.Get(AddressBufferLength);
                int encAddrBufLength = ChunkEncrypt(addrBytes, encAddrBufBytes);
                // ChunkEncrypt(addrBytes, AddressBufferLength, encAddrBufBytes, out encAddrBufLength);
                Debug.Assert(encAddrBufLength == AddressBufferLength + tagLen * 2 + ChunkLengthBytes);
                Array.Copy(encAddrBufBytes, 0, outbuf, outlength, encAddrBufLength);
                outlength += encAddrBufLength;
                logger.Debug($"_tcpRequestSent outlength {outlength}");
            }

            // handle other chunks
            while (true)
            {
                uint bufSize = (uint)buffer.Size;
                if (bufSize <= 0) return;
                var chunklength = (int)Math.Min(bufSize, ChunkLengthMask);
                byte[] chunkBytes = buffer.Get(chunklength);
                byte[] encChunkBytes = new byte[chunklength + tagLen * 2 + ChunkLengthBytes];
                int encChunkLength = ChunkEncrypt(chunkBytes, encChunkBytes);
                // ChunkEncrypt(chunkBytes, chunklength, encChunkBytes, out encChunkLength);
                Debug.Assert(encChunkLength == chunklength + tagLen * 2 + ChunkLengthBytes);
                Buffer.BlockCopy(encChunkBytes, 0, outbuf, outlength, encChunkLength);
                outlength += encChunkLength;
                logger.Debug("chunks enc outlength " + outlength);
                // check if we have enough space for outbuf
                if (outlength + TCPHandler.ChunkOverheadSize > TCPHandler.BufferSize)
                {
                    logger.Debug("enc outbuf almost full, giving up");
                    return;
                }
                bufSize = (uint)buffer.Size;
                if (bufSize <= 0)
                {
                    logger.Debug("No more data to encrypt, leaving");
                    return;
                }
            }
        }


        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Debug.Assert(buffer != null, "_decCircularBuffer != null");
            int bufSize;
            outlength = 0;
            // drop all into buffer
            buffer.Put(buf, 0, length);

            logger.Debug("---Start Decryption");
            if (!saltReady)
            {
                bufSize = buffer.Size;
                // check if we get the leading salt
                if (bufSize <= saltLen)
                {
                    // need more
                    return;
                }
                saltReady = true;
                byte[] salt = buffer.Get(saltLen);
                InitCipher(salt, false, false);
                logger.Debug("get salt len " + saltLen);
            }

            // handle chunks
            while (true)
            {
                bufSize = buffer.Size;
                // check if we have any data
                if (bufSize <= 0)
                {
                    logger.Debug("No data in _decCircularBuffer");
                    return;
                }

                // first get chunk length
                if (bufSize <= ChunkLengthBytes + tagLen)
                {
                    // so we only have chunk length and its tag?
                    return;
                }

                #region Chunk Decryption

                byte[] encLenBytes = buffer.Peek(ChunkLengthBytes + tagLen);

                // try to dec chunk len
                byte[] decChunkLenBytes = new byte[ChunkLengthBytes];
                CipherDecrypt(decChunkLenBytes, encLenBytes);
                ushort chunkLen = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(decChunkLenBytes, 0));
                if (chunkLen > ChunkLengthMask)
                {
                    // we get invalid chunk
                    logger.Error($"Invalid chunk length: {chunkLen}");
                    throw new CryptoErrorException();
                }
                logger.Debug("Get the real chunk len:" + chunkLen);
                bufSize = buffer.Size;
                if (bufSize < ChunkLengthBytes + tagLen /* we haven't remove them */+ chunkLen + tagLen)
                {
                    logger.Debug("No more data to decrypt one chunk");
                    return;
                }
                IncrementNonce();

                // we have enough data to decrypt one chunk
                // drop chunk len and its tag from buffer
                buffer.Skip(ChunkLengthBytes + tagLen);
                byte[] encChunkBytes = buffer.Get(chunkLen + tagLen);
                // byte[] decChunkBytes = CipherDecrypt2(encChunkBytes);

                int len =  CipherDecrypt(outbuf.AsSpan().Slice(outlength), encChunkBytes);
                IncrementNonce();

                #endregion

                // output to outbuf
                // decChunkBytes.CopyTo(outbuf, outlength);
                // Buffer.BlockCopy(decChunkBytes, 0, outbuf, outlength, (int)decChunkLen);
                outlength += len;
                logger.Debug("aead dec outlength " + outlength);
                if (outlength + 100 > TCPHandler.BufferSize)
                {
                    logger.Debug("dec outbuf almost full, giving up");
                    return;
                }
                bufSize = buffer.Size;
                // check if we already done all of them
                if (bufSize <= 0)
                {
                    logger.Debug("No data in _decCircularBuffer, already all done");
                    return;
                }
            }
        }

        #endregion

        #region UDP
        /// <summary>
        /// Perform AEAD UDP packet encryption
        /// </summary>
        /// payload => [salt][encrypted payload][tag]
        /// <param name="buf"></param>
        /// <param name="length"></param>
        /// <param name="outbuf"></param>
        /// <param name="outlength"></param>
        public override void EncryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            // Generate salt
            RNG.GetSpan(outbuf.AsSpan(0, saltLen));
            InitCipher(outbuf, true, true);
            outlength = saltLen + CipherEncrypt(
                buf.AsSpan(0, length),
                outbuf.AsSpan(saltLen, length + tagLen));

        }

        public override void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            InitCipher(buf, false, true);
            outlength = CipherDecrypt(outbuf, buf.AsSpan(saltLen, length - saltLen));
        }

        #endregion

        private int ChunkEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            if (plain.Length > ChunkLengthMask)
            {
                logger.Error("enc chunk too big");
                throw new CryptoErrorException();
            }

            byte[] lenbuf = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)plain.Length));
            int cipherLenSize = CipherEncrypt(lenbuf, cipher);
            IncrementNonce();
            int cipherDataSize = CipherEncrypt(plain, cipher.Slice(cipherLenSize));
            IncrementNonce();

            return cipherLenSize + cipherDataSize;
        }
    }
}