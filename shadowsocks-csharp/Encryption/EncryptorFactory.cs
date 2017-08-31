using System;
using System.Collections.Generic;
using System.Reflection;
using Shadowsocks.Encryption.AEAD;
using Shadowsocks.Encryption.Stream;

namespace Shadowsocks.Encryption
{
    public static class EncryptorFactory
    {
        private static Dictionary<string, Type> _registeredEncryptors = new Dictionary<string, Type>();

        private static readonly Type[] ConstructorTypes = {typeof(string), typeof(string)};

        static EncryptorFactory()
        {
            var AEADMbedTLSEncryptorSupportedCiphers = AEADMbedTLSEncryptor.SupportedCiphers();
            var AEADSodiumEncryptorSupportedCiphers = AEADSodiumEncryptor.SupportedCiphers();
            if (Sodium.AES256GCMAvailable)
            {
                // prefer to aes-256-gcm in libsodium
                AEADMbedTLSEncryptorSupportedCiphers.Remove("aes-256-gcm");
            }
            else
            {
                AEADSodiumEncryptorSupportedCiphers.Remove("aes-256-gcm");
            }

            foreach (string method in StreamMbedTLSEncryptor.SupportedCiphers())
            {
                _registeredEncryptors.Add(method, typeof(StreamMbedTLSEncryptor));
            }
            foreach (string method in StreamSodiumEncryptor.SupportedCiphers())
            {
                _registeredEncryptors.Add(method, typeof(StreamSodiumEncryptor));
            }
            foreach (string method in AEADMbedTLSEncryptorSupportedCiphers)
            {
                _registeredEncryptors.Add(method, typeof(AEADMbedTLSEncryptor));
            }
            foreach (string method in AEADSodiumEncryptorSupportedCiphers)
            {
                _registeredEncryptors.Add(method, typeof(AEADSodiumEncryptor));
            }
        }

        public static IEncryptor GetEncryptor(string method, string password)
        {
            if (method.IsNullOrEmpty())
            {
                method = "aes-256-cfb";
            }
            method = method.ToLowerInvariant();
            Type t = _registeredEncryptors[method];
            ConstructorInfo c = t.GetConstructor(ConstructorTypes);
            if (c == null) throw new System.Exception("Invalid ctor");
            IEncryptor result = (IEncryptor) c.Invoke(new object[] {method, password});
            return result;
        }
    }
}