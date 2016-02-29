using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace Shadowsocks.Encryption
{
    public abstract class IVEncryptor
        : EncryptorBase
    {
        public const int MAX_KEY_LENGTH = 64;
        public const int MAX_IV_LENGTH = 16;

        public const int ONETIMEAUTH_FLAG = 0x10;
        public const int ADDRTYPE_MASK = 0xF;

        public const int ONETIMEAUTH_BYTES = 10;

        public const int CLEN_BYTES = 2;
        public const int AUTH_BYTES = ONETIMEAUTH_BYTES + CLEN_BYTES;

        protected static byte[] tempbuf = new byte[MAX_INPUT_SIZE];

        protected Dictionary<string, int[]> ciphers;

        private static readonly ConcurrentDictionary<string, byte[]> CachedKeys = new ConcurrentDictionary<string, byte[]>();
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
        protected uint counter = 0;
        protected byte[] _keyBuffer = null;

        public IVEncryptor(string method, string password, bool onetimeauth, bool isudp)
            : base(method, password, onetimeauth, isudp)
        {
            InitKey(method, password);
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
            _key = CachedKeys.GetOrAdd(k, (nk) =>
            {
                byte[] passbuf = Encoding.UTF8.GetBytes(password);
                byte[] key = new byte[32];
                byte[] iv = new byte[16];
                bytesToKey(passbuf, key);
                return key;
            });
        }

        protected void bytesToKey(byte[] password, byte[] key)
        {
            byte[] result = new byte[password.Length + 16];
            int i = 0;
            byte[] md5sum = null;
            while (i < key.Length)
            {
                if (i == 0)
                {
                    md5sum = MbedTLS.MD5(password);
                }
                else
                {
                    md5sum.CopyTo(result, 0);
                    password.CopyTo(result, md5sum.Length);
                    md5sum = MbedTLS.MD5(result);
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

        protected int getHeadLen(byte[] buf, int length)
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

        protected byte[] genOnetimeAuthHash(byte[] msg, int msg_len)
        {
            byte[] auth = new byte[ONETIMEAUTH_BYTES];
            byte[] hash = new byte[20];
            byte[] auth_key = new byte[MAX_IV_LENGTH + MAX_KEY_LENGTH];
            Buffer.BlockCopy(_encryptIV, 0, auth_key, 0, ivLen);
            Buffer.BlockCopy(_key, 0, auth_key, ivLen, keyLen);
            Sodium.ss_sha1_hmac_ex(auth_key, (uint)(ivLen + keyLen),
                msg, 0, (uint)msg_len, hash);
            Buffer.BlockCopy(hash, 0, auth, 0, ONETIMEAUTH_BYTES);
            return auth;
        }

        protected void updateKeyBuffer()
        {
            if (_keyBuffer == null)
            {
                _keyBuffer = new byte[MAX_IV_LENGTH + 4];
                Buffer.BlockCopy(_encryptIV, 0, _keyBuffer, 0, ivLen);
            }

            byte[] counter_bytes = BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)counter));
            Buffer.BlockCopy(counter_bytes, 0, _keyBuffer, ivLen, 4);
            counter++;
        }

        protected byte[] genHash(byte[] buf, int offset, int len)
        {
            byte[] hash = new byte[20];
            updateKeyBuffer();
            Sodium.ss_sha1_hmac_ex(_keyBuffer, (uint)_keyBuffer.Length,
                buf, offset, (uint)len, hash);
            return hash;
        }

        protected void reactBuffer4TCP(byte[] buf, ref int length)
        {
            if (!_encryptIVSent)
            {
                int headLen = getHeadLen(buf, length);
                int dataLen = length - headLen;
                buf[0] |= ONETIMEAUTH_FLAG;
                byte[] hash = genOnetimeAuthHash(buf, headLen);
                Buffer.BlockCopy(buf, headLen, buf, headLen + ONETIMEAUTH_BYTES + AUTH_BYTES, dataLen);
                Buffer.BlockCopy(hash, 0, buf, headLen, ONETIMEAUTH_BYTES);
                hash = genHash(buf, headLen + ONETIMEAUTH_BYTES + AUTH_BYTES, dataLen);
                Buffer.BlockCopy(hash, 0, buf, headLen + ONETIMEAUTH_BYTES + CLEN_BYTES, ONETIMEAUTH_BYTES);
                byte[] lenBytes = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)dataLen));
                Buffer.BlockCopy(lenBytes, 0, buf, headLen + ONETIMEAUTH_BYTES, CLEN_BYTES);
                length = headLen + ONETIMEAUTH_BYTES + AUTH_BYTES + dataLen;
            }
            else
            {
                byte[] hash = genHash(buf, 0, length);
                Buffer.BlockCopy(buf, 0, buf, AUTH_BYTES, length);
                byte[] lenBytes = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)length));
                Buffer.BlockCopy(lenBytes, 0, buf, 0, CLEN_BYTES);
                Buffer.BlockCopy(hash, 0, buf, CLEN_BYTES, ONETIMEAUTH_BYTES);
                length += AUTH_BYTES;
            }
        }

        protected void reactBuffer4UDP(byte[] buf, ref int length)
        {
            buf[0] |= ONETIMEAUTH_FLAG;
            byte[] hash = genOnetimeAuthHash(buf, length);
            Buffer.BlockCopy(hash, 0, buf, length, ONETIMEAUTH_BYTES);
            length += ONETIMEAUTH_BYTES;
        }

        protected void reactBuffer(byte[] buf, ref int length)
        {
            if (OnetimeAuth && ivLen > 0)
            {
                if (!IsUDP)
                {
                    reactBuffer4TCP(buf, ref length);
                }
                else
                {
                    reactBuffer4UDP(buf, ref length);
                }
            }
        }

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            if (!_encryptIVSent)
            {
                randBytes(outbuf, ivLen);
                initCipher(outbuf, true);
                outlength = length + ivLen;
                reactBuffer(buf, ref length);
                _encryptIVSent = true;
                lock (tempbuf)
                {
                    cipherUpdate(true, length, buf, tempbuf);
                    outlength = length + ivLen;
                    Buffer.BlockCopy(tempbuf, 0, outbuf, ivLen, length);
                }
            }
            else
            {
                reactBuffer(buf, ref length);
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
