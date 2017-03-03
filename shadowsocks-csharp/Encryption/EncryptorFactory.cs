using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shadowsocks.Encryption
{
    public static class EncryptorFactory
    {
        private static Dictionary<string, Type> _registeredEncryptors;

        private static Type[] _constructorTypes = new Type[] { typeof(string), typeof(string), typeof(bool), typeof(bool) };

        static EncryptorFactory()
        {
            _registeredEncryptors = new Dictionary<string, Type>();
            foreach (string method in MbedTLSEncryptor.SupportedCiphers())
            {
                _registeredEncryptors.Add(method, typeof(MbedTLSEncryptor));
            }
            foreach (string method in SodiumEncryptor.SupportedCiphers())
            {
                _registeredEncryptors.Add(method, typeof(SodiumEncryptor));
            }
        }

        public static IEncryptor GetEncryptor(string method, string password, bool onetimeauth, bool isudp)
        {
            if (method.IsNullOrEmpty())
            {
                method = "aes-256-cfb";
            }
            method = method.ToLowerInvariant();
            Type t = _registeredEncryptors[method];
            ConstructorInfo c = t.GetConstructor(_constructorTypes);
            IEncryptor result = (IEncryptor)c.Invoke(new object[] { method, password, onetimeauth, isudp });
            return result;
        }
    }
}
