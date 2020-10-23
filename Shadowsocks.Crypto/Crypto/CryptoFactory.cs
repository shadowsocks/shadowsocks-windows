using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Shadowsocks.Common.Crypto;
using Shadowsocks.Common.Model;
using Shadowsocks.Crypto.AEAD;
using Shadowsocks.Crypto.Stream;

namespace Shadowsocks.Crypto
{
    public static class CryptoFactory
    {
        public static string DefaultCipher = "chacha20-ietf-poly1305";

        private static readonly Dictionary<string, Type> _registeredEncryptors = new Dictionary<string, Type>();
        private static readonly Dictionary<string, CipherInfo> ciphers = new Dictionary<string, CipherInfo>();
        private static readonly Type[] ConstructorTypes = { typeof(string), typeof(string) };

        static CryptoFactory()
        {
            foreach (var method in StreamPlainNativeCrypto.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamPlainNativeCrypto));
                }
            }
            foreach (var method in StreamRc4NativeCrypto.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamRc4NativeCrypto));
                }
            }
            foreach (var method in StreamAesCfbBouncyCastleCrypto.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamAesCfbBouncyCastleCrypto));
                }
            }
            foreach (var method in StreamChachaNaClCrypto.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamChachaNaClCrypto));
                }
            }


            foreach (var method in AEADAesGcmNativeCrypto.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(AEADAesGcmNativeCrypto));
                }
            }
            foreach (var method in AEADNaClCrypto.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(AEADNaClCrypto));
                }
            }
        }

        public static ICrypto GetEncryptor(string method, string password)
        {
            if (string.IsNullOrEmpty(method))
            {
                // todo
                //method = IoCManager.Container.Resolve<IDefaultCrypto>().GetDefaultMethod();
            }

            method = method.ToLowerInvariant();
            bool ok = _registeredEncryptors.TryGetValue(method, out Type t);
            if (!ok)
            {
                t = _registeredEncryptors[DefaultCipher];
            }

            ConstructorInfo c = t?.GetConstructor(ConstructorTypes) ??
                throw new TypeLoadException("can't load constructor");
            if (c == null) throw new System.Exception("Invalid ctor");
            ICrypto result = (ICrypto)c.Invoke(new object[] { method, password });
            return result;
        }

        public static string DumpRegisteredEncryptor()
        {
            var sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.AppendLine("-------------------------");
            sb.AppendLine("Registered Encryptor Info");
            foreach (var encryptor in _registeredEncryptors)
            {
                sb.AppendLine($"{ciphers[encryptor.Key].ToString(true)} => {encryptor.Value.Name}");
            }
            // use ----- instead of =======, so when user paste it to Github, it won't became title
            sb.AppendLine("-------------------------");
            return sb.ToString();
        }

        public static CipherInfo GetCipherInfo(string name)
        {
            // TODO: Replace cipher when required not exist
            return ciphers[name];
        }

        public static IEnumerable<CipherInfo> ListAvaliableCiphers()
        {
            return ciphers.Values;
        }
    }
}
