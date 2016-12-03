using System;

namespace Shadowsocks.Util.Sockets
{
    // Boyer-Moore string search
    public static class ByteSearch
    {
        public class SearchTarget
        {
            private readonly byte[] _needle;
            private readonly int[] _needleCharTable;
            private readonly int[] _needleOffsetTable;

            public SearchTarget(byte[] needle)
            {
                _needle = needle;
                _needleCharTable = MakeCharTable(needle);
                _needleOffsetTable = MakeOffsetTable(needle);
            }

            // Thread-safe
            public int SearchIn(byte[] haystack, int index, int length)
            {
                return IndexOf(haystack, index, length, _needle, _needleOffsetTable, _needleCharTable);
            }
        }

        private static int IndexOf(byte[] haystack, int index, int length, byte[] needle, int[] offsetTable, int[] charTable)
        {

            var end = index + length;
            for (int i = needle.Length - 1 + index, j; i < end;)
            {
                for (j = needle.Length - 1; needle[j] == haystack[i]; --i, --j)
                {
                    if (j == 0)
                    {
                        return i;
                    }
                }
                // i += needle.length - j; // For naive method
                i += Math.Max(offsetTable[needle.Length - 1 - j], charTable[haystack[i]]);
            }
            return -1;
        }

        /**
         * Makes the jump table based on the mismatched character information.
         */
        private static int[] MakeCharTable(byte[] needle)
        {
            const int ALPHABET_SIZE = 256;
            int[] table = new int[ALPHABET_SIZE];
            for (int i = 0; i < table.Length; ++i)
            {
                table[i] = needle.Length;
            }
            for (int i = 0; i < needle.Length - 1; ++i)
            {
                table[needle[i]] = needle.Length - 1 - i;
            }
            return table;
        }

        /**
         * Makes the jump table based on the scan offset which mismatch occurs.
         */
        private static int[] MakeOffsetTable(byte[] needle)
        {
            int[] table = new int[needle.Length];
            int lastPrefixPosition = needle.Length;
            for (int i = needle.Length - 1; i >= 0; --i)
            {
                if (IsPrefix(needle, i + 1))
                {
                    lastPrefixPosition = i + 1;
                }
                table[needle.Length - 1 - i] = lastPrefixPosition - i + needle.Length - 1;
            }
            for (int i = 0; i < needle.Length - 1; ++i)
            {
                int slen = SuffixLength(needle, i);
                table[slen] = needle.Length - 1 - i + slen;
            }
            return table;
        }

        /**
         * Is needle[p:end] a prefix of needle?
         */
        private static bool IsPrefix(byte[] needle, int p)
        {
            for (int i = p, j = 0; i < needle.Length; ++i, ++j)
            {
                if (needle[i] != needle[j])
                {
                    return false;
                }
            }
            return true;
        }

        /**
         * Returns the maximum length of the substring ends at p and is a suffix.
         */
        private static int SuffixLength(byte[] needle, int p)
        {
            int len = 0;
            for (int i = p, j = needle.Length - 1;
                     i >= 0 && needle[i] == needle[j]; --i, --j)
            {
                len += 1;
            }
            return len;
        }
    }
}
