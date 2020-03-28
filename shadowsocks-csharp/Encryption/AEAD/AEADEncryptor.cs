using NLog;
using Shadowsocks.Controller;
using Shadowsocks.Encryption.Exception;
using Shadowsocks.Encryption.Stream;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shadowsocks.Encryption.AEAD
{
    public abstract class AEADEncryptor : EncryptorBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        // We are using the same saltLen and keyLen
        private const string Info = "ss-subkey";
        private static readonly byte[] InfoBytes = Encoding.ASCII.GetBytes(Info);

        // every connection should create its own buffer
        private readonly byte[] buffer = new byte[65536];
        private int bufPtr = 0;

        public const int ChunkLengthBytes = 2;
        public const uint ChunkLengthMask = 0x3FFFu;

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
            CipherInfo = GetCiphers()[method.ToLower()];
            cipherFamily = CipherInfo.Type;
            AEADCipherParameter parameter = (AEADCipherParameter)CipherInfo.CipherParameter;
            keyLen = parameter.KeySize;
            saltLen = parameter.SaltSize;
            tagLen = parameter.TagSize;
            nonceLen = parameter.NonceSize;

            InitKey(password);

            salt = new byte[saltLen];
            // Initialize all-zero nonce for each connection
            nonce = new byte[nonceLen];
            logger.Dump($"masterkey {instanceId}", masterKey, keyLen);
            logger.Dump($"nonce {instanceId}", nonce, keyLen);
        }

        protected abstract Dictionary<string, CipherInfo> GetCiphers();

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

            StreamEncryptor.LegacyDeriveKey(passbuf, masterKey, keyLen);
            // init session key
            sessionKey = new byte[keyLen];
        }

        public virtual void InitCipher(byte[] salt, bool isEncrypt)
        {
            this.salt = new byte[saltLen];
            Array.Copy(salt, this.salt, saltLen);

            CryptoUtils.HKDF(keyLen, masterKey, salt, InfoBytes).CopyTo(sessionKey, 0);

            logger.Dump($"salt {instanceId}", salt, saltLen);
            logger.Dump($"sessionkey {instanceId}", sessionKey, keyLen);
        }

        public abstract int CipherEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher);
        public abstract int CipherDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher);

        #region TCP

        [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.AggressiveOptimization)]
        public override int Encrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            // push data
            Span<byte> tmp = buffer.AsSpan(0, plain.Length + bufPtr);
            plain.CopyTo(tmp.Slice(bufPtr));

            int outlength = 0;
            if (!saltReady)
            {
                saltReady = true;
                // Generate salt
                byte[] saltBytes = RNG.GetBytes(saltLen);
                InitCipher(saltBytes, true);
                saltBytes.CopyTo(cipher);
                outlength = saltLen;
            }

            if (!tcpRequestSent)
            {
                tcpRequestSent = true;

                // read addr byte to encrypt
                int encAddrBufLength = ChunkEncrypt(tmp.Slice(0, AddressBufferLength), cipher.Slice(outlength));
                tmp = tmp.Slice(AddressBufferLength);
                outlength += encAddrBufLength;
            }

            // handle other chunks
            while (true)
            {
                // calculate next chunk size
                int bufSize = tmp.Length;
                if (bufSize <= 0)
                {
                    return outlength;
                }

                int chunklength = (int)Math.Min(bufSize, ChunkLengthMask);
                // read next chunk
                int encChunkLength = ChunkEncrypt(tmp.Slice(0, chunklength), cipher.Slice(outlength));
                tmp = tmp.Slice(chunklength);
                outlength += encChunkLength;

                // check if we have enough space for outbuf
                // if not, keep buf for next run, at this condition, buffer is not empty
                if (outlength + TCPHandler.ChunkOverheadSize > TCPHandler.BufferSize)
                {
                    logger.Debug("enc outbuf almost full, giving up");

                    // write rest data to head of shared buffer
                    tmp.CopyTo(buffer);
                    bufPtr = tmp.Length;

                    return outlength;
                }
                // check if buffer empty
                bufSize = tmp.Length;
                if (bufSize <= 0)
                {
                    logger.Debug("No more data to encrypt, leaving");
                    return outlength;
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.AggressiveOptimization)]
        public override int Decrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            int outlength = 0;
            // drop all into buffer
            Span<byte> tmp = buffer.AsSpan(0, cipher.Length + bufPtr);
            cipher.CopyTo(tmp.Slice(bufPtr));
            int bufSize = tmp.Length;

            logger.Debug("---Start Decryption");
            if (!saltReady)
            {
                // check if we get the leading salt
                if (bufSize <= saltLen)
                {
                    // need more, write back cache
                    tmp.CopyTo(buffer);
                    bufPtr = tmp.Length;
                    return outlength;
                }
                saltReady = true;

                // buffer.Get(saltLen);
                byte[] salt = tmp.Slice(0, saltLen).ToArray();
                tmp = tmp.Slice(saltLen);

                InitCipher(salt, false);
                logger.Debug("get salt len " + saltLen);
            }

            // handle chunks
            while (true)
            {
                bufSize = tmp.Length;
                // check if we have any data
                if (bufSize <= 0)
                {
                    logger.Debug("No data in buffer");
                    return outlength;
                }

                // first get chunk length
                if (bufSize <= ChunkLengthBytes + tagLen)
                {
                    // so we only have chunk length and its tag?
                    // wait more
                    tmp.CopyTo(buffer);
                    bufPtr = tmp.Length;
                    return outlength;
                }

                int len = ChunkDecrypt(plain.Slice(outlength), tmp);
                if (len <= 0)
                {
                    // no chunk decrypted
                    tmp.CopyTo(buffer);
                    bufPtr = tmp.Length;
                    return outlength;
                }
                // drop decrypted data
                tmp = tmp.Slice(ChunkLengthBytes + tagLen + len + tagLen);
                outlength += len;

                logger.Debug("aead dec outlength " + outlength);
                if (outlength + 100 > TCPHandler.BufferSize)
                {
                    logger.Debug("dec outbuf almost full, giving up");
                    tmp.CopyTo(buffer);
                    bufPtr = tmp.Length;
                    return outlength;
                }
                bufSize = tmp.Length;
                // check if we already done all of them
                if (bufSize <= 0)
                {
                    logger.Debug("No data in _decCircularBuffer, already all done");
                    return outlength;
                }
            }
        }

        #endregion

        #region UDP
        [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.AggressiveOptimization)]
        public override int EncryptUDP(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            RNG.GetSpan(cipher.Slice(0, saltLen));
            InitCipher(cipher.Slice(0, saltLen).ToArray(), true);
            return saltLen + CipherEncrypt(plain, cipher.Slice(saltLen));
        }

        [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.AggressiveOptimization)]
        public override int DecryptUDP(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            InitCipher(cipher.Slice(0, saltLen).ToArray(), false);
            return CipherDecrypt(plain, cipher.Slice(saltLen));
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private int ChunkEncrypt(ReadOnlySpan<byte> plain, Span<byte> cipher)
        {
            if (plain.Length > ChunkLengthMask)
            {
                logger.Error("enc chunk too big");
                throw new CryptoErrorException();
            }

            byte[] lenbuf = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)plain.Length));
            int cipherLenSize = CipherEncrypt(lenbuf, cipher);
            CryptoUtils.SodiumIncrement(nonce);
            int cipherDataSize = CipherEncrypt(plain, cipher.Slice(cipherLenSize));
            CryptoUtils.SodiumIncrement(nonce);

            return cipherLenSize + cipherDataSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private int ChunkDecrypt(Span<byte> plain, ReadOnlySpan<byte> cipher)
        {
            // try to dec chunk len
            byte[] chunkLengthByte = new byte[ChunkLengthBytes];
            CipherDecrypt(chunkLengthByte, cipher.Slice(0, ChunkLengthBytes + tagLen));
            ushort chunkLength = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(chunkLengthByte, 0));
            if (chunkLength > ChunkLengthMask)
            {
                // we get invalid chunk
                logger.Error($"Invalid chunk length: {chunkLength}");
                throw new CryptoErrorException();
            }
            logger.Debug("Get the real chunk len:" + chunkLength);
            int bufSize = cipher.Length;
            if (bufSize < ChunkLengthBytes + tagLen /* we haven't remove them */+ chunkLength + tagLen)
            {
                logger.Debug("No data to decrypt one chunk");
                return 0;
            }
            CryptoUtils.SodiumIncrement(nonce);
            // we have enough data to decrypt one chunk
            // drop chunk len and its tag from buffer
            int len = CipherDecrypt(plain, cipher.Slice(ChunkLengthBytes + tagLen, chunkLength + tagLen));
            CryptoUtils.SodiumIncrement(nonce);
            return len;
        }
    }
}