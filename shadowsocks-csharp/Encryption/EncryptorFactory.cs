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

        private static readonly Type[] ConstructorTypes = { typeof(string), typeof(string) };

        static EncryptorFactory()
        {
            foreach (string method in StreamNativeEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(StreamNativeEncryptor));
            }

            foreach (string method in AEADNativeEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(AEADNativeEncryptor));
            }

            foreach (string method in AEADBouncyCastleEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(AEADBouncyCastleEncryptor));
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