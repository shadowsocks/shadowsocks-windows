using System;
using System.Text;

namespace Shadowsocks.Util
{
    public static class Base64
    {
        public static string DecodeBase64(string val)
        {
            byte[] bytes = null;
            string data = val;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    bytes = Convert.FromBase64String(val);
                }
                catch (FormatException)
                {
                    val += "=";
                }
            }
            if (bytes != null)
            {
                data = Encoding.UTF8.GetString(bytes);
            }
            return data;
        }

        public static string EncodeUrlSafeBase64(string val)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(val)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public static string DecodeUrlSafeBase64(string val)
        {
            byte[] bytes = null;
            string data = val.Replace('-', '+').Replace('_', '/').PadRight(val.Length + (4 - val.Length % 4) % 4, '=');
            try
            {
                bytes = Convert.FromBase64String(data);
            }
            catch (FormatException)
            {
            }
            if (bytes != null)
            {
                return Encoding.UTF8.GetString(bytes);
            }
            return val;
        }
    }
}
