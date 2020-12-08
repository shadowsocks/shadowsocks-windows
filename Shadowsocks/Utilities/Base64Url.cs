using System;
using System.Text;

namespace Shadowsocks.Utilities
{
    public static class Base64Url
    {
        public static string Encode(string data) => Encode(Encoding.UTF8.GetBytes(data));
        
        public static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static string DecodeToString(string base64url) => Encoding.UTF8.GetString(DecodeToBytes(base64url));

        public static byte[] DecodeToBytes(string base64url)
        {
            var base64string = base64url.Replace('_', '/').Replace('-', '+');
            base64string = base64string.PadRight(base64string.Length + (4 - base64string.Length % 4) % 4, '=');
            return Convert.FromBase64String(base64string);
        }
    }
}
