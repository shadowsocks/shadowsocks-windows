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

        // for UDP only
        protected static byte[] _udpTmpBuf = new byte[65536];

        // every connection should create its own buffer
        private ByteCircularBuffer buffer = new ByteCircularBuffer(MAX_INPUT_SIZE * 2);
        // private ByteCircularBuffer buffer = new ByteCircularBuffer(MAX_INPUT_SIZE * 2);

        public const int CHUNK_LEN_BYTES = 2;
        public const uint CHUNK_LEN_MASK = 0x3FFFu;

        protected Dictionary<string, CipherInfo> ciphers;

        protected string _method;
        protected CipherFamily _cipher;
        protected CipherInfo CipherInfo;
        protected static byte[] masterKey = null;
        protected byte[] sessionKey;
        protected int keyLen;
        protected int saltLen;
        protected int tagLen;
        protected int nonceLen;

        protected byte[] salt;

        protected object _nonceIncrementLock = new object();
        protected byte[] nonce;

        // Is first packet
        protected bool saltReady;

        // Is first chunk(tcp request)
        protected bool tcpRequestSent;

        public AEADEncryptor(string method, string password)
            : base(method, password)
        {
            InitEncryptorInfo(method);
            InitKey(password);
            // Initialize all-zero nonce for each connection
            nonce = new byte[nonceLen];
            nonce = new byte[nonceLen];
        }

        protected abstract Dictionary<string, CipherInfo> getCiphers();

        protected void InitEncryptorInfo(string method)
        {
            method = method.ToLower();
            _method = method;
            ciphers = getCiphers();
            CipherInfo = ciphers[_method];
            _cipher = CipherInfo.Type;
            var parameter = (AEADCipherParameter)CipherInfo.CipherParameter;
            keyLen = parameter.KeySize;
            saltLen = parameter.SaltSize;
            tagLen = parameter.TagSize;
            nonceLen = parameter.NonceSize;
        }

        protected void InitKey(string password)
        {
            byte[] passbuf = Encoding.UTF8.GetBytes(password);
            // init master key
            if (masterKey == null) masterKey = new byte[keyLen];
            if (masterKey.Length != keyLen) Array.Resize(ref masterKey, keyLen);
            DeriveKey(passbuf, masterKey, keyLen);
            // init session key
            if (sessionKey == null) sessionKey = new byte[keyLen];
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plaintext">Input, plain text</param>
        /// <param name="plen">plaintext.Length</param>
        /// <param name="ciphertext">Output, allocated by caller, tag space included,
        /// length = plaintext.Length + tagLen, [enc][tag] order</param>
        /// <param name="clen">Should be same as ciphertext.Length</param>
        public abstract void cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen);

        public abstract int CipherEncrypt(Span<byte> plain, Span<byte> cipher);
        public abstract int CipherDecrypt(Span<byte> plain, Span<byte> cipher);

        // plain -> cipher + tag
        [Obsolete]
        public abstract byte[] CipherEncrypt2(byte[] plain);
        // cipher + tag -> plain
        [Obsolete]
        public abstract byte[] CipherDecrypt2(byte[] cipher);

        public (Memory<byte>, Memory<byte>) GetCipherTextAndTagMem(byte[] cipher)
        {
            var mc = cipher.AsMemory();
            var clen = mc.Length - tagLen;
            var c = mc.Slice(0, clen);
            var t = mc.Slice(clen);

            return (c, t);
        }
        public (byte[], byte[]) GetCipherTextAndTag(byte[] cipher)
        {
            var (c, t) = GetCipherTextAndTagMem(cipher);
            return (c.ToArray(), t.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ciphertext">Cipher text in [enc][tag] order</param>
        /// <param name="clen">ciphertext.Length</param>
        /// <param name="plaintext">Output plain text may with additional data allocated by caller</param>
        /// <param name="plen">Output, should be used plain text length</param>
        public abstract void cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen);

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
                byte[] encAddrBufBytes = new byte[AddressBufferLength + tagLen * 2 + CHUNK_LEN_BYTES];
                byte[] addrBytes = buffer.Get(AddressBufferLength);
                int encAddrBufLength = ChunkEncrypt(addrBytes, encAddrBufBytes);
                // ChunkEncrypt(addrBytes, AddressBufferLength, encAddrBufBytes, out encAddrBufLength);
                Debug.Assert(encAddrBufLength == AddressBufferLength + tagLen * 2 + CHUNK_LEN_BYTES);
                Array.Copy(encAddrBufBytes, 0, outbuf, outlength, encAddrBufLength);
                outlength += encAddrBufLength;
                logger.Debug($"_tcpRequestSent outlength {outlength}");
            }

            // handle other chunks
            while (true)
            {
                uint bufSize = (uint)buffer.Size;
                if (bufSize <= 0) return;
                var chunklength = (int)Math.Min(bufSize, CHUNK_LEN_MASK);
                byte[] chunkBytes = buffer.Get(chunklength);
                byte[] encChunkBytes = new byte[chunklength + tagLen * 2 + CHUNK_LEN_BYTES];
                int encChunkLength = ChunkEncrypt(chunkBytes, encChunkBytes);
                // ChunkEncrypt(chunkBytes, chunklength, encChunkBytes, out encChunkLength);
                Debug.Assert(encChunkLength == chunklength + tagLen * 2 + CHUNK_LEN_BYTES);
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
                if (bufSize <= CHUNK_LEN_BYTES + tagLen)
                {
                    // so we only have chunk length and its tag?
                    return;
                }

                #region Chunk Decryption

                byte[] encLenBytes = buffer.Peek(CHUNK_LEN_BYTES + tagLen);

                // try to dec chunk len
                byte[] decChunkLenBytes = CipherDecrypt2(encLenBytes);
                ushort chunkLen = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(decChunkLenBytes, 0));
                if (chunkLen > CHUNK_LEN_MASK)
                {
                    // we get invalid chunk
                    logger.Error($"Invalid chunk length: {chunkLen}");
                    throw new CryptoErrorException();
                }
                logger.Debug("Get the real chunk len:" + chunkLen);
                bufSize = buffer.Size;
                if (bufSize < CHUNK_LEN_BYTES + tagLen /* we haven't remove them */+ chunkLen + tagLen)
                {
                    logger.Debug("No more data to decrypt one chunk");
                    return;
                }
                IncrementNonce();

                // we have enough data to decrypt one chunk
                // drop chunk len and its tag from buffer
                buffer.Skip(CHUNK_LEN_BYTES + tagLen);
                byte[] encChunkBytes = buffer.Get(chunkLen + tagLen);
                byte[] decChunkBytes = CipherDecrypt2(encChunkBytes);
                IncrementNonce();

                #endregion

                // output to outbuf
                decChunkBytes.CopyTo(outbuf, outlength);
                // Buffer.BlockCopy(decChunkBytes, 0, outbuf, outlength, (int)decChunkLen);
                outlength += decChunkBytes.Length;
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
            //RNG.GetBytes(outbuf, saltLen);
            RNG.GetSpan(outbuf.AsSpan().Slice(0, saltLen));
            InitCipher(outbuf, true, true);
            //uint olen = 0;
            lock (_udpTmpBuf)
            {
                //cipherEncrypt(buf, (uint)length, _udpTmpBuf, ref olen);
                var plain = buf.AsSpan().Slice(0, length).ToArray(); // mmp
                var cipher = CipherEncrypt2(plain);
                //Debug.Assert(olen == length + tagLen);
                Buffer.BlockCopy(cipher, 0, outbuf, saltLen, length + tagLen);
                //Buffer.BlockCopy(_udpTmpBuf, 0, outbuf, saltLen, (int)olen);
                outlength = saltLen + cipher.Length;
            }
        }

        public override void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            InitCipher(buf, false, true);
            //uint olen = 0;
            lock (_udpTmpBuf)
            {
                // copy remaining data to first pos
                Buffer.BlockCopy(buf, saltLen, buf, 0, length - saltLen);
                byte[] b = buf.AsSpan().Slice(0, length - saltLen).ToArray();
                byte[] o = CipherDecrypt2(b);
                //cipherDecrypt(buf, (uint)(length - saltLen), _udpTmpBuf, ref olen);
                Buffer.BlockCopy(o, 0, outbuf, 0, o.Length);
                outlength = o.Length;
            }
        }

        #endregion

        private int ChunkEncrypt(Span<byte> plain, Span<byte> cipher)
        {
            if (plain.Length > CHUNK_LEN_MASK)
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