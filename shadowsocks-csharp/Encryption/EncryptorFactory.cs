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
        private static Dictionary<string, Type> _registeredEncryptors = new Dictionary<string, Type>();
        private static Dictionary<string, CipherInfo> ciphers = new Dictionary<string, CipherInfo>();
        private static readonly Type[] ConstructorTypes = { typeof(string), typeof(string) };

        static EncryptorFactory()
        {
            foreach (var method in StreamTableNativeEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamTableNativeEncryptor));
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
            foreach (var method in StreamRc4NativeEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method.Key))
                {
                    ciphers.Add(method.Key, method.Value);
                    _registeredEncryptors.Add(method.Key, typeof(StreamRc4NativeEncryptor));
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
            Type t = _registeredEncryptors[method];

            ConstructorInfo c = t.GetConstructor(ConstructorTypes);
            if (c == null) throw new System.Exception("Invalid ctor");
            IEncryptor result = (IEncryptor)c.Invoke(new object[] { method, password });
            return result;
        }

        public static string DumpRegisteredEncryptor()
        {
            var sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.AppendLine("=========================");
            sb.AppendLine("Registered Encryptor Info");
            foreach (var encryptor in _registeredEncryptors)
            {
                sb.AppendLine(String.Format("{0}=>{1}", encryptor.Key, encryptor.Value.Name));
            }

            sb.AppendLine("=========================");
            return sb.ToString();
        }
    }
}