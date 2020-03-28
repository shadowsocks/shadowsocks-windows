using NLog;
using Shadowsocks.Controller;
using Shadowsocks.Encryption.Exception;
using Shadowsocks.Encryption.Stream;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Shadowsocks.Encryption.AEAD
{
    public abstract class AEADEncryptor : EncryptorBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        // We are using the same saltLen and keyLen
        private const string Info = "ss-subkey";
        private static readonly byte[] InfoBytes = Encoding.ASCII.GetBytes(Info);

        // every connection should create its own buffer
        private byte[] sharedBuffer = new byte[65536];
        private int bufPtr = 0;

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
            AEADCipherParameter parameter = (AEADCipherParameter)CipherInfo.CipherParameter;
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
            if (masterKey == null)
            {
                masterKey = new byte[keyLen];
            }

            if (masterKey.Length != keyLen)
            {
                Array.Resize(ref masterKey, keyLen);
            }

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

        public abstract int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher);
        public abstract int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher);

        #region TCP

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            // push data
            buf.CopyTo(sharedBuffer, bufPtr);
            Span<byte> tmp = sharedBuffer.AsSpan(0, length + bufPtr);

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

                // read addr byte to encrypt
                byte[] addrBytes = tmp.Slice(0, AddressBufferLength).ToArray();
                tmp = tmp.Slice(AddressBufferLength);

                int encAddrBufLength = ChunkEncrypt(addrBytes, encAddrBufBytes);

                Array.Copy(encAddrBufBytes, 0, outbuf, outlength, encAddrBufLength);
                outlength += encAddrBufLength;
                logger.Debug($"_tcpRequestSent outlength {outlength}");
            }

            // handle other chunks
            while (true)
            {
                // calculate next chunk size
                int bufSize = tmp.Length;
                if (bufSize <= 0)
                {
                    return;
                }

                int chunklength = (int)Math.Min(bufSize, ChunkLengthMask);
                // read next chunk
                byte[] chunkBytes = tmp.Slice(0, chunklength).ToArray();
                tmp = tmp.Slice(chunklength);

                byte[] encChunkBytes = new byte[chunklength + tagLen * 2 + ChunkLengthBytes];
                int encChunkLength = ChunkEncrypt(chunkBytes, encChunkBytes);

                Buffer.BlockCopy(encChunkBytes, 0, outbuf, outlength, encChunkLength);
                outlength += encChunkLength;
                logger.Debug("chunks enc outlength " + outlength);
                // check if we have enough space for outbuf
                // if not, keep buf for next run, at this condition, buffer is not empty
                if (outlength + TCPHandler.ChunkOverheadSize > TCPHandler.BufferSize)
                {
                    logger.Debug("enc outbuf almost full, giving up");

                    // write rest data to head of shared buffer
                    tmp.CopyTo(sharedBuffer);
                    bufPtr = tmp.Length;

                    return;
                }
                // check if buffer empty
                bufSize = tmp.Length;
                if (bufSize <= 0)
                {
                    logger.Debug("No more data to encrypt, leaving");
                    return;
                }
            }
        }


        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            outlength = 0;
            // drop all into buffer
            buf.CopyTo(sharedBuffer, bufPtr);
            Span<byte> tmp = buf.AsSpan(0, length + bufPtr);
            int bufSize = tmp.Length;

            logger.Debug("---Start Decryption");
            if (!saltReady)
            {
                // check if we get the leading salt
                if (bufSize <= saltLen)
                {
                    // need more, write back cache
                    tmp.CopyTo(sharedBuffer);
                    bufPtr = tmp.Length;
                    return;
                }
                saltReady = true;

                // buffer.Get(saltLen);
                byte[] salt = tmp.Slice(0, saltLen).ToArray();
                tmp = tmp.Slice(saltLen);

                InitCipher(salt, false, false);
                logger.Debug("get salt len " + saltLen);
            }

            // handle chunks
            while (true)
            {
                bufSize = tmp.Length;
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
                    // wait more
                    tmp.CopyTo(sharedBuffer);
                    bufPtr = tmp.Length;
                    return;
                }

                #region Chunk Decryption

                // byte[] encLenBytes = buffer.Peek(ChunkLengthBytes + tagLen);
                byte[] encLenBytes = tmp.Slice(0, ChunkLengthBytes + tagLen).ToArray();

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
                bufSize = tmp.Length;
                if (bufSize < ChunkLengthBytes + tagLen /* we haven't remove them */+ chunkLen + tagLen)
                {
                    logger.Debug("No more data to decrypt one chunk");
                    // write back length data
                    tmp.CopyTo(sharedBuffer);
                    bufPtr = tmp.Length;
                    return;
                }
                IncrementNonce();
                // we have enough data to decrypt one chunk
                // drop chunk len and its tag from buffer
                // buffer.Skip(ChunkLengthBytes + tagLen);
                tmp = tmp.Slice(ChunkLengthBytes + tagLen);
                // byte[] encChunkBytes = buffer.Get(chunkLen + tagLen);
                byte[] encChunkBytes = tmp.Slice(0, chunkLen + tagLen).ToArray();
                tmp = tmp.Slice(chunkLen + tagLen);

                int len = CipherDecrypt(outbuf.AsSpan(outlength), encChunkBytes);
                IncrementNonce();

                #endregion

                // output to outbuf
                outlength += len;
                logger.Debug("aead dec outlength " + outlength);
                if (outlength + 100 > TCPHandler.BufferSize)
                {
                    logger.Debug("dec outbuf almost full, giving up");
                    tmp.CopyTo(sharedBuffer);
                    bufPtr = tmp.Length;
                    return;
                }
                bufSize = tmp.Length;
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

        private int ChunkEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
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