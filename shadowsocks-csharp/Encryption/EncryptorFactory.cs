using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Shadowsocks.Encryption.AEAD;
using Shadowsocks.Encryption.Stream;

namespace Shadowsocks.Encryption
{
    public static class EncryptorFactory
    {
        public static string DefaultCipher = "chacha20-ietf-poly1305";

        private static readonly Dictionary<string, Type> _registeredEncryptors = new Dictionary<string, Type>();
        private static readonly Dictionary<string, CipherInfo> ciphers = new Dictionary<string, CipherInfo>();
        private static readonly Type[] ConstructorTypes = { typeof(string), typeof(string) };

        static EncryptorFactory()
        {
            foreach (var method in StreamPlainNativeEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamPlainNativeEncryptor));
                }
            }
            foreach (var method in StreamRc4NativeEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamRc4NativeEncryptor));
                }
            }
            foreach (var method in StreamAesCfbBouncyCastleEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamAesCfbBouncyCastleEncryptor));
                }
            }
            foreach (var method in StreamChachaNaClEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamChachaNaClEncryptor));
                }
            }


            foreach (var method in AEADAesGcmNativeEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(AEADAesGcmNativeEncryptor));
                }
            }
            foreach (var method in AEADNaClEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(AEADNaClEncryptor));
                }
            }
        }

        public static IEncryptor GetEncryptor(string method, string password)
        {
            if (method.IsNullOrEmpty())
            {
                method = Model.Server.DefaultMethod;
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
            IEncryptor result = (IEncryptor)c.Invoke(new object[] { method, password });
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