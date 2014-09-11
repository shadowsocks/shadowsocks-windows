
namespace shadowsocks_csharp.Encrypt
{
    public static class EncryptorFactory
    {
        public static IEncryptor GetEncryptor(string method, string password)
        {
            if (string.IsNullOrEmpty(method) || method.ToLowerInvariant() == "table")
            {
                return new TableEncryptor(method, password);
            }

            if (method.ToLowerInvariant() == "rc4")
            {
                return new Rc4Encryptor(method, password);
            }

            return new OpensslEncryptor(method, password);
        }
    }
}
