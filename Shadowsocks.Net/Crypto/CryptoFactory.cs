using Shadowsocks.Net.Crypto.AEAD;
using Shadowsocks.Net.Crypto.Stream;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks.Net.Crypto;

public static class CryptoFactory
{
    public static string DefaultCipher = "chacha20-ietf-poly1305";

    private static readonly Dictionary<string, Type> _registeredEncryptors = [];
    private static readonly Dictionary<string, CipherInfo> _ciphers = [];
    private static readonly Type[] _constructorTypes = { typeof(string), typeof(string) };

    static CryptoFactory()
    {
        foreach (var method in StreamPlainNativeCrypto.SupportedCiphers())
        {
            if (!_registeredEncryptors.ContainsKey(method.Key))
            {
                _ciphers.Add(method.Key, method.Value);
                _registeredEncryptors.Add(method.Key, typeof(StreamPlainNativeCrypto));
            }
        }

        foreach (var method in StreamCryptoBaseCrypto.SupportedCiphers())
        {
            if (!_registeredEncryptors.ContainsKey(method.Key))
            {
                _ciphers.Add(method.Key, method.Value);
                _registeredEncryptors.Add(method.Key, typeof(StreamCryptoBaseCrypto));
            }
        }

        foreach (var method in AEADCryptoBaseCrypto.SupportedCiphers())
        {
            if (_registeredEncryptors.ContainsKey(method.Key)) { continue; }
            _ciphers.Add(method.Key, method.Value);
            _registeredEncryptors.Add(method.Key, typeof(AEADCryptoBaseCrypto));
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
        var ok = _registeredEncryptors.TryGetValue(method, out var t);
        if (!ok)
        {
            t = _registeredEncryptors[DefaultCipher];
        }

        var c = t?.GetConstructor(_constructorTypes) ??
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
            sb.AppendLine($"{_ciphers[encryptor.Key].ToString(true)} => {encryptor.Value.Name}");
        }
        // use ----- instead of =======, so when user paste it to Github, it won't became title
        sb.AppendLine("-------------------------");
        return sb.ToString();
    }

    public static CipherInfo GetCipherInfo(string name)
    // TODO: Replace cipher when required not exist
    => _ciphers[name];

    public static IEnumerable<CipherInfo> ListAvaliableCiphers() => _ciphers.Values;
}