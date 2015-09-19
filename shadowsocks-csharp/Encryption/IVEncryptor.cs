using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace Shadowsocks.Encryption
{
    public abstract class IVEncryptor
        : EncryptorBase
    {
        public const int ONETIMEAUTH_FLAG = 0x10;
        public const int ADDRTYPE_MASK = 0xF;
        public const int ONETIMEAUTH_BYTES = 16;
        public const int CRC_BUF_LEN = 128;
        public const int CRC_BYTES = 2;

        protected static byte[] tempbuf = new byte[MAX_INPUT_SIZE];

        protected Dictionary<string, int[]> ciphers;

        private static readonly Dictionary<string, byte[]> CachedKeys = new Dictionary<string, byte[]>();
        protected byte[] _encryptIV;
        protected byte[] _decryptIV;
        protected bool _decryptIVReceived;
        protected bool _encryptIVSent;
        protected int _encryptIVOffset = 0;
        protected int _decryptIVOffset = 0;
        protected string _method;
        protected int _cipher;
        protected int[] _cipherInfo;
        protected byte[] _key;
        protected int keyLen;
        protected int ivLen;
        protected byte[] crc_buf;
        protected int crc_idx = 0;

        public IVEncryptor(string method, string password, bool onetimeauth)
            : base(method, password, onetimeauth)
        {
            InitKey(method, password);
            if (OnetimeAuth)
            {
                crc_buf = new byte[CRC_BUF_LEN];
            }
        }

        protected abstract Dictionary<string, int[]> getCiphers();

        protected void InitKey(string method, string password)
        {
            method = method.ToLower();
            _method = method;
            string k = method + ":" + password;
            ciphers = getCiphers();
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

        protected void bytesToKey(byte[] password, byte[] key)
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

        protected static void randBytes(byte[] buf, int length)
        {
            byte[] temp = new byte[length];
            RNGCryptoServiceProvider rngServiceProvider = new RNGCryptoServiceProvider();
            rngServiceProvider.GetBytes(temp);
            temp.CopyTo(buf, 0);
        }

        protected virtual void initCipher(byte[] iv, bool isCipher)
        {
            if (ivLen > 0)
            {
                if (isCipher)
                {
                    _encryptIV = new byte[ivLen];
                    Array.Copy(iv, _encryptIV, ivLen);
                }
                else
                {
                    _decryptIV = new byte[ivLen];
                    Array.Copy(iv, _decryptIV, ivLen);
                }
            }
        }

        protected abstract void cipherUpdate(bool isCipher, int length, byte[] buf, byte[] outbuf);

        protected int GetSSHeadLength(byte[] buf, int length)
        {
            int len = 0;
            int atyp = length > 0 ? (buf[0] & ADDRTYPE_MASK) : 0;
            if (atyp == 1)
            {
                len = 7; // atyp (1 bytes) + ipv4 (4 bytes) + port (2 bytes)
            }
            else if (atyp == 3 && length > 1)
            {
                int nameLen = buf[1];
                len = 4 + nameLen; // atyp (1 bytes) + name length (1 bytes) + name (n bytes) + port (2 bytes)
            }
            else if (atyp == 4)
            {
                len = 19; // atyp (1 bytes) + ipv6 (16 bytes) + port (2 bytes)
            }
            if (len == 0 || len > length)
                throw new Exception($"invalid header with addr type {atyp}");
            return len;
        }

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            if (!_encryptIVSent)
            {
                _encryptIVSent = true;
                randBytes(outbuf, ivLen);
                initCipher(outbuf, true);
                outlength = length + ivLen;
                lock (tempbuf)
                {
                    if (OnetimeAuth)
                    {
                        lock(crc_buf)
                        {
                            int headLen = GetSSHeadLength(buf, length);
                            int data_len = length - headLen;
                            Buffer.BlockCopy(buf, headLen, buf, headLen + ONETIMEAUTH_BYTES, data_len);
                            buf[0] |= ONETIMEAUTH_FLAG;
                            byte[] auth = new byte[ONETIMEAUTH_BYTES];
                            Sodium.ss_onetimeauth(auth, buf, headLen, _encryptIV, ivLen, _key, keyLen);
                            Buffer.BlockCopy(auth, 0, buf, headLen, ONETIMEAUTH_BYTES);
                            int buf_offset = headLen + ONETIMEAUTH_BYTES;
                            int rc = Sodium.ss_gen_crc(buf, ref buf_offset, ref data_len, crc_buf, ref crc_idx, buf.Length);
                            if (rc != 0)
                                throw new Exception("failed to generate crc");
                            length = headLen + ONETIMEAUTH_BYTES + data_len;
                        }
                    }
                    cipherUpdate(true, length, buf, tempbuf);
                    outlength = length + ivLen;
                    Buffer.BlockCopy(tempbuf, 0, outbuf, ivLen, length);
                }
            }
            else
            {
                if (OnetimeAuth)
                {
                    lock(crc_buf)
                    {
                        int buf_offset = 0;
                        int rc = Sodium.ss_gen_crc(buf, ref buf_offset, ref length, crc_buf, ref crc_idx, buf.Length);
                        if (rc != 0)
                            throw new Exception("failed to generate crc");
                    }
                }
                outlength = length;
                cipherUpdate(true, length, buf, outbuf);
            }
        }

        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            if (!_decryptIVReceived)
            {
                _decryptIVReceived = true;
                initCipher(buf, false);
                outlength = length - ivLen;
                lock (tempbuf)
                {
                    // C# could be multi-threaded
                    Buffer.BlockCopy(buf, ivLen, tempbuf, 0, length - ivLen);
                    cipherUpdate(false, length - ivLen, tempbuf, outbuf);
                }
            }
            else
            {
                outlength = length;
                cipherUpdate(false, length, buf, outbuf);
            }
        }

    }
}
