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
        public const int MAX_KEY_LENGTH = 64;
        public const int MAX_IV_LENGTH = 16;

        public const int ONETIMEAUTH_FLAG = 0x10;
        public const int ADDRTYPE_MASK = 0xF;

        public const int ONETIMEAUTH_BYTES = 16;
        public const int ONETIMEAUTH_KEYBYTES = 32;

        public const int HASH_BYTES = 4;
        public const int CLEN_BYTES = 2;
        public const int AUTH_BYTES = HASH_BYTES + CLEN_BYTES;

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
        protected uint counter = 0;
        protected byte[] _keyBuffer = null;

        public IVEncryptor(string method, string password, bool onetimeauth)
            : base(method, password, onetimeauth)
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

        protected int ss_headlen(byte[] buf, int length)
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

        protected int ss_onetimeauth(byte[] auth, byte[] msg, int msg_len)
        {
            byte[] auth_key = new byte[ONETIMEAUTH_KEYBYTES];
            byte[] auth_bytes = new byte[MAX_IV_LENGTH + MAX_KEY_LENGTH];
            Buffer.BlockCopy(_encryptIV, 0, auth_bytes, 0, ivLen);
            Buffer.BlockCopy(_key, 0, auth_bytes, ivLen, keyLen);
            Sodium.crypto_generichash(auth_key, ONETIMEAUTH_KEYBYTES, auth_bytes, (ulong)(ivLen + keyLen), null, 0);
            return Sodium.crypto_onetimeauth(auth, msg, (ulong)msg_len, auth_key);
        }

        protected void ss_gen_hash(byte[] buf, ref int offset, ref int len, int buf_size)
        {
            int size = len + AUTH_BYTES;
            if (buf_size < (size + offset))
                throw new Exception("failed to generate hash:  buffer size insufficient");

            if (_keyBuffer == null)
            {
                _keyBuffer = new byte[MAX_IV_LENGTH + 4];
                Buffer.BlockCopy(_encryptIV, 0, _keyBuffer, 0, ivLen);
            }

            byte[] counter_bytes = BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)counter));
            Buffer.BlockCopy(counter_bytes, 0, _keyBuffer, ivLen, 4);

            byte[] hash = new byte[HASH_BYTES];
            byte[] tmp = new byte[len];
            Buffer.BlockCopy(buf, offset, tmp, 0, len);
            Sodium.crypto_generichash(hash, HASH_BYTES, tmp, (ulong)len, _keyBuffer, (uint)_keyBuffer.Length);

            Buffer.BlockCopy(buf, offset, buf, offset + AUTH_BYTES, len);
            Buffer.BlockCopy(hash, 0, buf, offset + CLEN_BYTES, HASH_BYTES);
            byte[] clen = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)len));
            Buffer.BlockCopy(clen, 0, buf, offset, CLEN_BYTES);

            counter++;
            len += AUTH_BYTES;
            offset += len;
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
                        int headLen = ss_headlen(buf, length);
                        int len = length - headLen;
                        Buffer.BlockCopy(buf, headLen, buf, headLen + ONETIMEAUTH_BYTES, len);
                        buf[0] |= ONETIMEAUTH_FLAG;
                        byte[] auth = new byte[ONETIMEAUTH_BYTES];
                        ss_onetimeauth(auth, buf, headLen);
                        Buffer.BlockCopy(auth, 0, buf, headLen, ONETIMEAUTH_BYTES);
                        int offset = headLen + ONETIMEAUTH_BYTES;
                        ss_gen_hash(buf, ref offset, ref len, buf.Length);
                        length = headLen + ONETIMEAUTH_BYTES + len;
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
                    int offset = 0;
                    ss_gen_hash(buf, ref offset, ref length, buf.Length);
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
