
using System;
using System.Collections.Generic;
using System.Reflection;
namespace Shadowsocks.Encryption
{
    public static class EncryptorFactory
    {
        private static Dictionary<string, Type> _registeredEncryptors;
        private static List<string> _registeredEncryptorNames;

        private static Type[] _constructorTypes = new Type[] { typeof(string), typeof(string) };

        static EncryptorFactory()
        {
            _registeredEncryptors = new Dictionary<string, Type>();
            _registeredEncryptorNames = new List<string>();
            if (LibcryptoEncryptor.isSupport())
            {
                LibcryptoEncryptor.InitAviable();
                foreach (string method in LibcryptoEncryptor.SupportedCiphers())
                {
                    _registeredEncryptorNames.Add(method);
                    _registeredEncryptors.Add(method, typeof(LibcryptoEncryptor));
                }
            }
            else
            {
                foreach (string method in PolarSSLEncryptor.SupportedCiphers())
                {
                    _registeredEncryptorNames.Add(method);
                    _registeredEncryptors.Add(method, typeof(PolarSSLEncryptor));
                }
            }
            foreach (string method in SodiumEncryptor.SupportedCiphers())
            {
                _registeredEncryptorNames.Add(method);
                _registeredEncryptors.Add(method, typeof(SodiumEncryptor));
            }
        }

        public static List<string> GetEncryptor()
        {
            return _registeredEncryptorNames;
        }

        public static IEncryptor GetEncryptor(string method, string password)
        {
            if (string.IsNullOrEmpty(method))
            {
                method = "aes-256-cfb";
            }
            method = method.ToLowerInvariant();
            Type t = _registeredEncryptors[method];
            ConstructorInfo c = t.GetConstructor(_constructorTypes);
            IEncryptor result = (IEncryptor)c.Invoke(new object[] { method, password });
            return result;
        }
    }
}
