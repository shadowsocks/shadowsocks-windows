namespace Shadowsocks.Encryption.Exception
{
    public class CryptoErrorException : System.Exception
    {
        public CryptoErrorException()
        {
        }

        public CryptoErrorException(string msg) : base(msg)
        {
        }

        public CryptoErrorException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}