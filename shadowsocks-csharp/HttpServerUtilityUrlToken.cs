using System;
using System.Collections.Generic;
using System.Text;

namespace Shadowsocks
{
    /// <summary>
    /// HttpServerUtility URL Token のエンコード及びデコードを行うクラス。
    /// https://docs.microsoft.com/ja-jp/dotnet/api/system.web.httpserverutility.urltokenencode
    /// https://docs.microsoft.com/ja-jp/dotnet/api/system.web.httpserverutility.urltokendecode
    /// </summary>
    /// <remarks>
    /// HttpServerUtility URL Token 形式は、パディング無し base64url にパディング数を文字として追記した文字列です。
    /// 例えば、<c>0x00</c> は <c>AA2</c> になります。
    /// </remarks>
    public static class HttpServerUtilityUrlToken
    {
#if NETSTANDARD2_0
        private static readonly byte[] EmptyBytes = Array.Empty<byte>();
#else
        private static readonly byte[] EmptyBytes = new byte[0];
#endif

        /// <summary>
        /// <see cref="byte"/> 配列を HttpServerUtility URL Token にエンコードします。
        /// </summary>
        /// <param name="bytes">エンコード対象の <see cref="byte"/> 配列。</param>
        /// <returns>HttpServerUtility URL Token エンコード文字列。<paramref name="bytes"/> の長さが <c>0</c> の場合は空文字列を返します。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <c>null</c>.</exception>
        public static string Encode(byte[] bytes)
        {
            if (bytes == null) { throw new ArgumentNullException(nameof(bytes)); }

            return Encode(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// <see cref="byte"/> 配列を HttpServerUtility URL Token にエンコードします。
        /// </summary>
        /// <param name="bytes">エンコード対象の <see cref="byte"/> 配列。</param>
        /// <param name="offset">エンコードの開始位置を示すオフセット。</param>
        /// <param name="length">エンコード対象の要素の数。</param>
        /// <returns>HttpServerUtility URL Token エンコード文字列。<paramref name="length"/> が <c>0</c> の場合は空文字列を返します。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> または <paramref name="length"/> が負の値です。
        /// または <paramref name="offset"/> と <paramref name="length"/> を加算した値が <paramref name="bytes"/> の長さを超えています。
        /// </exception>
        public static string Encode(byte[] bytes, int offset, int length)
        {
            if (bytes == null) { throw new ArgumentNullException(nameof(bytes)); }

            var encoded = Encode(bytes, offset, length, padding: false);
            if (encoded.Length == 0) { return ""; }

            var paddingLen = unchecked(~encoded.Length + 1) & 0b11;
            encoded += paddingLen;

            return encoded;
        }
        /// <summary>
        /// <see cref="byte"/> 配列を base64url にエンコードします。
        /// </summary>
        /// <param name="bytes">エンコード対象の <see cref="byte"/> 配列。</param>
        /// <param name="offset">エンコードの開始位置を示すオフセット。</param>
        /// <param name="length">エンコード対象の要素の数。</param>
        /// <param name="padding">パディングをする場合は <c>true</c>、それ以外は <c>false</c>。既定値は <c>false</c>。</param>
        /// <returns>base64url エンコード文字列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> または <paramref name="length"/> が負の値です。
        /// または <paramref name="offset"/> と <paramref name="length"/> を加算した値が <paramref name="bytes"/> の長さを超えています。
        /// </exception>
        public static string Encode(byte[] bytes, int offset, int length, bool padding = false)
        {
            var encoded = Convert.ToBase64String(bytes, offset, length);

            if (!padding)
            {
                encoded = encoded.TrimEnd('=');
            }

            return encoded
                .Replace('+', '-')
                .Replace('/', '_')
                ;
        }

        /// <summary>
        /// HttpServerUtility URL Token 文字列を <see cref="byte"/> 配列にデコードします。
        /// </summary>
        /// <param name="encoded">HttpServerUtility URL Token にエンコードされた文字列。</param>
        /// <returns>デコード後の <see cref="byte"/> 配列。<paramref name="encoded"/> が空文字列の場合は <see cref="byte"/> の空配列を返します。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="encoded"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="encoded"/> が HttpServerUtility URL Token 文字列ではありません。</exception>
        public static byte[] Decode(string encoded)
        {
            if (encoded == null) { throw new ArgumentNullException(nameof(encoded)); }

            if (!TryDecode(encoded, out var result)) { throw new FormatException("HttpServerUtility URL Token 文字列ではありません。"); }
            return result;
        }

        /// <summary>
        /// HttpServerUtility URL Token でエンコードされた文字列をデコードします。
        /// </summary>
        /// <param name="encoded">HttpServerUtility URL Token エンコードされた文字列。</param>
        /// <param name="result">デコード後の <see cref="byte"/> 配列。<paramref name="encoded"/> が空文字列の場合は <see cref="byte"/> の空配列が設定されます。失敗した場合は <c>null</c>。</param>
        /// <returns>デコードに成功した場合は <c>true</c>、それ以外は <c>false</c>。</returns>
        public static bool TryDecode(string encoded, out byte[] result)
        {
            if (encoded == null) { goto Failure; }
            if (encoded.Length == 0)
            {
                result = EmptyBytes;
                return true;
            }

            var paddingLen = encoded[encoded.Length - 1] - '0';
            if (paddingLen < 0 || paddingLen > 3) { goto Failure; }

            var base64Str = encoded
                .Substring(0, encoded.Length - 1)
                .Replace('-', '+')
                .Replace('_', '/');

            if (paddingLen > 0)
            {
                base64Str += new string('=', paddingLen);
            }

            try
            {
                result = Convert.FromBase64String(base64Str);
                return true;
            }
            catch (FormatException) { goto Failure; }

            Failure:
            result = null;
            return false;
        }
    }
}
