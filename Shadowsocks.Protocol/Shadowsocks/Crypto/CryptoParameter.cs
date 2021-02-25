using System;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    public struct CryptoParameter
    {
        public Type Crypto;
        public int KeySize; // key size = salt size
        public int NonceSize; // reused as iv size
        public int TagSize;

        public ICrypto GetCrypto()
        {
            var ctor = Crypto.GetConstructor(new[] { typeof(CryptoParameter) }) ??
                  throw new TypeLoadException("can't load constructor");
            return (ICrypto)ctor.Invoke(new object[] { this });
        }

        public bool IsAead => TagSize > 0;
    }
}
