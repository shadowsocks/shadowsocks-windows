using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#if EXPOSE_EVERYTHING || EXPOSE_STRINGEX
public
#endif
static partial class StringEx
{
#pragma warning disable 1591

    public static StringComparison GlobalDefaultComparison { get; set; } = StringComparison.Ordinal;

    [ThreadStatic]
    private static StringComparison? _DefaultComparison;
    public static StringComparison DefaultComparison
    {
        get { return _DefaultComparison ?? GlobalDefaultComparison; }
        set { _DefaultComparison = value; }
    }

    #region basic String methods

    public static bool IsNullOrEmpty(this string value)
        => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace(this string value)
        => string.IsNullOrWhiteSpace(value);

    public static bool IsWhiteSpace(this string value)
    {
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c)) continue;

            return false;
        }
        return true;
    }

#if !PCL
    public static string IsInterned(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return string.IsInterned(value);
    }

    public static string Intern(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return string.Intern(value);
    }
#endif

#if UNSAFE
    public static unsafe string ToLowerForASCII(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return value;

        value = string.Copy(value);
        fixed (char* low = value)
        {
            var end = low + value.Length;
            for (var p = low; p < end; p++)
            {
                var c = *p;
                if (c < 'A' || c > 'Z')
                    continue;
                *p = (char)(c + 0x20);
            }
        }
        return value;
    }

    public static unsafe string ToUpperForASCII(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return value;

        value = string.Copy(value);
        fixed (char* low = value)
        {
            var end = low + value.Length;
            for (var p = low; p < end; p++)
            {
                var c = *p;
                if (c < 'a' || c > 'z')
                    continue;
                *p = (char)(c - 0x20);
            }
        }
        return value;
    }
#else
    public static string ToLowerForASCII(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return value;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c < 'A' || c > 'Z')
                sb.Append(c);
            else
                sb.Append((char)(c + 0x20));
        }
        return sb.ToString();
    }

    public static string ToUpperForASCII(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return value;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c < 'a' || c > 'z')
                sb.Append(c);
            else
                sb.Append((char)(c - 0x20));
        }
        return sb.ToString();
    }
#endif

    #endregion

    #region comparing

    #region Is

    public static bool Is(this string a, string b)
        => string.Equals(a, b, DefaultComparison);
    public static bool Is(this string a, string b, StringComparison comparisonType)
        => string.Equals(a, b, comparisonType);

    #endregion

    #region BeginWith

    public static bool BeginWith(this string s, char c)
    {
        if (s.IsNullOrEmpty()) return false;
        return s[0] == c;
    }
    public static bool BeginWithAny(this string s, IEnumerable<char> chars)
    {
        if (s.IsNullOrEmpty()) return false;
        return chars.Contains(s[0]);
    }
    public static bool BeginWithAny(this string s, params char[] chars)
        => s.BeginWithAny(chars.AsEnumerable());

    public static bool BeginWith(this string a, string b)
    {
        if (a == null || b == null) return false;

        return a.StartsWith(b, DefaultComparison);
    }
    public static bool BeginWith(this string a, string b, StringComparison comparisonType)
    {
        if (a == null || b == null) return false;

        return a.StartsWith(b, comparisonType);
    }
#if !PCL
    public static bool BeginWith(this string a, string b, bool ignoreCase, CultureInfo culture)
    {
        if (a == null || b == null) return false;

        return a.StartsWith(b, ignoreCase, culture);
    }
#endif

    #endregion

    #region FinishWith

    public static bool FinishWith(this string s, char c)
    {
        if (s.IsNullOrEmpty()) return false;
        return s.Last() == c;
    }
    public static bool FinishWithAny(this string s, IEnumerable<char> chars)
    {
        if (s.IsNullOrEmpty()) return false;
        return chars.Contains(s.Last());
    }
    public static bool FinishWithAny(this string s, params char[] chars)
        => s.FinishWithAny(chars.AsEnumerable());

    public static bool FinishWith(this string a, string b)
    {
        if (a == null || b == null) return false;

        return a.EndsWith(b, DefaultComparison);
    }
    public static bool FinishWith(this string a, string b, StringComparison comparisonType)
    {
        if (a == null || b == null) return false;

        return a.EndsWith(b, comparisonType);
    }
#if !PCL
    public static bool FinishWith(this string a, string b, bool ignoreCase, CultureInfo culture)
    {
        if (a == null || b == null) return false;

        return a.EndsWith(b, ignoreCase, culture);
    }
#endif

    #endregion

    #endregion

    #region ToLines

    public static IEnumerable<string> ToLines(this TextReader reader)
    {
        string line;
        while ((line = reader.ReadLine()) != null)
            yield return line;
    }
    public static IEnumerable<string> NonEmptyLines(this TextReader reader)
    {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line == "") continue;
            yield return line;
        }
    }
    public static IEnumerable<string> NonWhiteSpaceLines(this TextReader reader)
    {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.IsWhiteSpace()) continue;
            yield return line;
        }
    }

    #endregion

    #region others

    private static readonly char[][] Quotes = new[]
    {
        "\"\"",
        "''",
        "“”",
        "‘’",
        "『』",
        "「」",
        "〖〗",
        "【】",
    }.Select(s => s.ToCharArray()).ToArray();
    public static string Enquote(this string value)
    {
        if (value == null)
            return "(null)";

        foreach (var pair in Quotes)
        {
            if (value.IndexOfAny(pair) < 0)
                return pair[0] + value + pair[1];
        }

        return '"' + value.Replace("\\", @"\\").Replace("\"", @"\""") + '"';
    }

    public static string Replace(this string value, string find, string rep, StringComparison comparsionType)
    {
        if (find.IsNullOrEmpty())
            throw new ArgumentException(null, nameof(find));
        if (rep == null)
            rep = "";
        if (value.IsNullOrEmpty())
            return value;

        var sb = new StringBuilder(value.Length);

        var last = 0;
        var len = find.Length;
        var idx = value.IndexOf(find, DefaultComparison);
        while (idx != -1)
        {
            sb.Append(value.Substring(last, idx - last));
            sb.Append(rep);
            idx += len;

            last = idx;
            idx = value.IndexOf(find, idx, comparsionType);
        }
        sb.Append(value.Substring(last));

        return sb.ToString();
    }
    public static string ReplaceEx(this string value, string find, string rep)
        => value.Replace(find, rep, DefaultComparison);

    #endregion
}
