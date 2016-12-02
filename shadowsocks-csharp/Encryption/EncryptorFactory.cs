
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
            foreach (string method in NoneEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptorNames.Contains(method))
                {
                    _registeredEncryptorNames.Add(method);
                    _registeredEncryptors.Add(method, typeof(NoneEncryptor));
                }
            }

            {
                foreach (string method in MbedTLSEncryptor.SupportedCiphers())
                {
                    if (!_registeredEncryptorNames.Contains(method))
                    {
                        _registeredEncryptorNames.Add(method);
                        _registeredEncryptors.Add(method, typeof(MbedTLSEncryptor));
                    }
                }
            }
            if (LibcryptoEncryptor.isSupport())
            {
                LibcryptoEncryptor.InitAviable();
                foreach (string method in LibcryptoEncryptor.SupportedCiphers())
                {
                    if (!_registeredEncryptorNames.Contains(method))
                    {
                        _registeredEncryptorNames.Add(method);
                        _registeredEncryptors.Add(method, typeof(LibcryptoEncryptor));
                    }
                }
            }
            foreach (string method in SodiumEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptorNames.Contains(method))
                {
                    _registeredEncryptorNames.Add(method);
                    _registeredEncryptors.Add(method, typeof(SodiumEncryptor));
                }
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

        public static EncryptorInfo GetEncryptorInfo(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                method = "aes-256-cfb";
            }
            method = method.ToLowerInvariant();
            Type t = _registeredEncryptors[method];
            ConstructorInfo c = t.GetConstructor(_constructorTypes);
            IEncryptor result = (IEncryptor)c.Invoke(new object[] { method, "0" });
            EncryptorInfo info = result.getInfo();
            result.Dispose();
            return info;
        }
    }
}
