using System;
using System.Collections.Generic;

namespace Shadowsocks.Encryption.Stream
{
    class PlainEncryptor
        : EncryptorBase, IDisposable
    {
        const int CIPHER_NONE = 1;

        private static Dictionary<string, EncryptorInfo> _ciphers = new Dictionary<string, EncryptorInfo> {
            { "plain", new EncryptorInfo("PLAIN", 0, 0, CIPHER_NONE) },
            { "none", new EncryptorInfo("PLAIN", 0, 0, CIPHER_NONE) }
        };

        public PlainEncryptor(string method, string password) : base(method, password)
        {
        }

        public static List<string> SupportedCiphers()
        {
            return new List<string>(_ciphers.Keys);
        }

        protected Dictionary<string, EncryptorInfo> getCiphers()
        {
            return _ciphers;
        }

        #region TCP

        public override void Encrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Buffer.BlockCopy(buf, 0, outbuf, 0, length);
            outlength = length;
        }

        public override void Decrypt(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Buffer.BlockCopy(buf, 0, outbuf, 0, length);
            outlength = length;
        }

        #endregion

        #region UDP

        public override void EncryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Buffer.BlockCopy(buf, 0, outbuf, 0, length);
            outlength = length;
        }

        public override void DecryptUDP(byte[] buf, int length, byte[] outbuf, out int outlength)
        {
            Buffer.BlockCopy(buf, 0, outbuf, 0, length);
            outlength = length;
        }

        #endregion


        #region IDisposable

        private bool _disposed;

        // instance based lock
        private readonly object _lock = new object();

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PlainEncryptor()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            if (disposing)
            {
                // free managed objects
            }
        }

        #endregion

    }
}
