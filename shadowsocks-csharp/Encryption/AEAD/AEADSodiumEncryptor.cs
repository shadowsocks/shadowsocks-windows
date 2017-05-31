using System;
using System.Collections.Generic;
using System.Diagnostics;
using Shadowsocks.Controller;
using Shadowsocks.Encryption.Exception;

namespace Shadowsocks.Encryption.AEAD
{
    public class AEADSodiumEncryptor
        : AEADEncryptor, IDisposable
    {
        private const int CIPHER_CHACHA20IETFPOLY1305 = 1;
        private const int CIPHER_AES256GCM = 2;

        private byte[] _sodiumEncSubkey;
        private byte[] _sodiumDecSubkey;

        public AEADSodiumEncryptor(string method, string password)
            : base(method, password)
        {
            _sodiumEncSubkey = new byte[keyLen];
            _sodiumDecSubkey = new byte[keyLen];
        }

        private static Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo>
        {
            {"chacha20-ietf-poly1305", new EncryptorInfo(32, 32, 12, 16, CIPHER_CHACHA20IETFPOLY1305)},
            {"aes-256-gcm", new EncryptorInfo(32, 32, 12, 16, CIPHER_AES256GCM)},
        };

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected override Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        public override void InitCipher(byte[] salt, bool isEncrypt, bool isUdp)
        {
            base.InitCipher(salt, isEncrypt, isUdp);
            DeriveSessionKey(isEncrypt ? _encryptSalt : _decryptSalt, _Masterkey,
                isEncrypt ? _sodiumEncSubkey : _sodiumDecSubkey);
        }


        public override int cipherEncrypt(byte[] plaintext, uint plen, byte[] ciphertext, ref uint clen)
        {
            Debug.Assert(_sodiumEncSubkey != null);
            // buf: all plaintext
            // outbuf: ciphertext + tag
            int ret;
            ulong encClen = 0;
            Logging.Dump("_encNonce before enc", _encNonce, nonceLen);
            Logging.Dump("_sodiumEncSubkey", _sodiumEncSubkey, keyLen);
            Logging.Dump("before cipherEncrypt: plain", plaintext, (int) plen);
            switch (_cipher)
            {
                case CIPHER_CHACHA20IETFPOLY1305:
                    ret = Sodium.crypto_aead_chacha20poly1305_ietf_encrypt(ciphertext, ref encClen,
                        plaintext, (ulong) plen,
                        null, 0,
                        null, _encNonce,
                        _sodiumEncSubkey);
                    break;
                case CIPHER_AES256GCM:
                    ret = Sodium.crypto_aead_aes256gcm_encrypt(ciphertext, ref encClen,
                        plaintext, (ulong)plen,
                        null, 0,
                        null, _encNonce,
                        _sodiumEncSubkey);
                    break;
                default:
                    throw new System.Exception("not implemented");
            }
            if (ret != 0) throw new CryptoErrorException();
            Logging.Dump("after cipherEncrypt: cipher", ciphertext, (int) encClen);
            clen = (uint) encClen;
            return ret;
        }

        public override int cipherDecrypt(byte[] ciphertext, uint clen, byte[] plaintext, ref uint plen)
        {
            Debug.Assert(_sodiumDecSubkey != null);
            // buf: ciphertext + tag
            // outbuf: plaintext
            int ret;
            ulong decPlen = 0;
            Logging.Dump("_decNonce before dec", _decNonce, nonceLen);
            Logging.Dump("_sodiumDecSubkey", _sodiumDecSubkey, keyLen);
            Logging.Dump("before cipherDecrypt: cipher", ciphertext, (int) clen);
            switch (_cipher)
            {
                case CIPHER_CHACHA20IETFPOLY1305:
                    ret = Sodium.crypto_aead_chacha20poly1305_ietf_decrypt(plaintext, ref decPlen,
                        null,
                        ciphertext, (ulong) clen,
                        null, 0,
                        _decNonce, _sodiumDecSubkey);
                    break;
                case CIPHER_AES256GCM:
                    ret = Sodium.crypto_aead_aes256gcm_decrypt(plaintext, ref decPlen,
                        null,
                        ciphertext, (ulong)clen,
                        null, 0,
                        _decNonce, _sodiumDecSubkey);
                    break;
                default:
                    throw new System.Exception("not implemented");
            }

            if (ret != 0) throw new CryptoErrorException();
            Logging.Dump("after cipherDecrypt: plain", plaintext, (int) decPlen);
            plen = (uint) decPlen;
            return ret;
        }

        public override void Dispose()
        {
        }
    }
}