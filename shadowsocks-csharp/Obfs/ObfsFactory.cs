using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shadowsocks.Obfs
{
    public static class ObfsFactory
    {
        private static Dictionary<string, Type> _registeredObfs;

        private static Type[] _constructorTypes = new Type[] { typeof(string) };

        static ObfsFactory()
        {
            _registeredObfs = new Dictionary<string, Type>();
            foreach (string method in Plain.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(Plain));
            }
            foreach (string method in HttpSimpleObfs.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(HttpSimpleObfs));
            }
            foreach (string method in TlsAuthObfs.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(TlsAuthObfs));
            }
            foreach (string method in VerifySimpleObfs.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(VerifySimpleObfs));
            }
            foreach (string method in VerifyDeflateObfs.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(VerifyDeflateObfs));
            }
            foreach (string method in VerifySHA1Obfs.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(VerifySHA1Obfs));
            }
            foreach (string method in AuthSimple.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(AuthSimple));
            }
            foreach (string method in AuthSHA1.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(AuthSHA1));
            }
            foreach (string method in AuthSHA1V2.SupportedObfs())
            {
                _registeredObfs.Add(method, typeof(AuthSHA1V2));
            }
        }

        public static IObfs GetObfs(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                method = "plain";
            }
            method = method.ToLowerInvariant();
            Type t = _registeredObfs[method];
            ConstructorInfo c = t.GetConstructor(_constructorTypes);
            IObfs result = (IObfs)c.Invoke(new object[] { method });
            return result;
        }
    }
}
