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

            // XXX: sequence matters, OpenSSL > Sodium > MbedTLS
            foreach (string method in StreamOpenSSLEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(StreamOpenSSLEncryptor));
            }

            foreach (string method in StreamSodiumEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(StreamSodiumEncryptor));
            }

            foreach (string method in StreamMbedTLSEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(StreamMbedTLSEncryptor));
            }


            foreach (string method in AEADOpenSSLEncryptor.SupportedCiphers())
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(AEADOpenSSLEncryptor));
            }

            foreach (string method in AEADSodiumEncryptorSupportedCiphers)
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(AEADSodiumEncryptor));
            }

            foreach (string method in AEADMbedTLSEncryptorSupportedCiphers)
            {
                if (!_registeredEncryptors.ContainsKey(method))
                    _registeredEncryptors.Add(method, typeof(AEADMbedTLSEncryptor));
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