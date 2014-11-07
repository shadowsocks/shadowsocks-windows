
namespace shadowsocks.Encrypt
{
    public static class EncryptorFactory
    {
        public static IEncryptor GetEncryptor(string method, string password)
        {
            if (string.IsNullOrEmpty(method) || method.ToLowerInvariant() == "table")
            {
                return new TableEncryptor(method, password);
            }

            return new PolarSSLEncryptor(method, password);
        }
    }
}
