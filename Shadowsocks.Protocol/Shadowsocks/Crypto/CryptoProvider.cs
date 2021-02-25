using System;
using System.Collections.Generic;

namespace Shadowsocks.Protocol.Shadowsocks.Crypto
{
    static class CryptoProvider
    {
        static Dictionary<string, CryptoParameter> parameters = new Dictionary<string, CryptoParameter>
        {
            ["aes-256-gcm"] = new CryptoParameter { KeySize = 32, NonceSize = 12, TagSize = 16, Crypto = typeof(AeadAesGcmCrypto) },
            ["aes-192-gcm"] = new CryptoParameter { KeySize = 24, NonceSize = 12, TagSize = 16, Crypto = typeof(AeadAesGcmCrypto) },
            ["aes-128-gcm"] = new CryptoParameter { KeySize = 16, NonceSize = 12, TagSize = 16, Crypto = typeof(AeadAesGcmCrypto) },
            ["none"] = new CryptoParameter { KeySize = 0, NonceSize = 0, TagSize = 0, Crypto = typeof(UnsafeNoneCrypto) }
        };

        public static CryptoParameter GetCrypto(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                // todo
                //method = IoCManager.Container.Resolve<IDefaultCrypto>().GetDefaultMethod();
            }

            method = method.ToLowerInvariant();
            var ok = parameters.TryGetValue(method, out var t);
            if (!ok)
            {
                //t = parameters[DefaultCipher];
                throw new NotImplementedException();
            }
            return t;
        }
    }
}
