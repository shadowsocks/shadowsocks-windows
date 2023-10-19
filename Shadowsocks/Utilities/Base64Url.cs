using System;
using System.Text;

namespace Shadowsocks.Utilities;

public static class Base64Url
{
    public static string Encode(string data) => Encode(Encoding.UTF8.GetBytes(data));

    public static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string DecodeToString(string base64Url) => Encoding.UTF8.GetString(DecodeToBytes(base64Url));

    public static byte[] DecodeToBytes(string base64Url)
    {
        var base64String = base64Url.Replace('_', '/').Replace('-', '+');
        base64String = base64String.PadRight(base64String.Length + (4 - base64String.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64String);
    }
}