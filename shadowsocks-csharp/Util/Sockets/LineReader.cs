using System;
using System.Text;

namespace Shadowsocks.Util.Sockets
{
    public class LineReader
    {
        private readonly WrappedSocket _socket;
        private readonly Func<string, object, bool> _onLineRead;
        private readonly Action<Exception, object> _onException;
        private readonly Action<byte[], int, int, object> _onFinish;
        private readonly Encoding _encoding;
        // private readonly string _delimiter;
        private readonly byte[] _delimiterBytes;
        private readonly int[] _delimiterSearchCharTable;
        private readonly int[] _delimiterSearchOffsetTable;
        private readonly object _state;

        private readonly byte[] _lineBuffer;

        private int _bufferDataIndex;
        private int _bufferDataLength;

        public LineReader(WrappedSocket socket, byte[] firstPackge, int index, int length,
            Func<string, object, bool> onLineRead, Action<Exception, object> onException,
            Action<byte[], int, int, object> onFinish,
            Encoding encoding, string delimiter, int maxLineBytes,
            object state)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
            if (onLineRead == null)
            {
                throw new ArgumentNullException(nameof(onLineRead));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            if (delimiter == null)
            {
                throw new ArgumentNullException(nameof(delimiter));
            }

            if (maxLineBytes < length)
            {
                throw new ArgumentException("Line buffer length can't less than first package length!", nameof(maxLineBytes));
            }

            if (length > 0)
            {
                if (firstPackge == null)
                {
                    throw new ArgumentNullException(nameof(firstPackge));
                }
            }

            _socket = socket;
            _onLineRead = onLineRead;
            _onException = onException;
            _onFinish = onFinish;
            _encoding = encoding;
            // _delimiter = delimiter;
            _state = state;

            // decode delimiter
            _delimiterBytes = encoding.GetBytes(delimiter);

            if (_delimiterBytes.Length == 0)
            {
                throw new ArgumentException("Too short!", nameof(delimiter));
            }

            if (maxLineBytes < _delimiterBytes.Length)
            {
                throw new ArgumentException("Too small!", nameof(maxLineBytes));
            }

            _delimiterSearchCharTable = MakeCharTable(_delimiterBytes);
            _delimiterSearchOffsetTable = MakeOffsetTable(_delimiterBytes);

            _lineBuffer = new byte[maxLineBytes];

            if (length > 0)
            {
                // process first package
                Array.Copy(firstPackge, index, _lineBuffer, 0, length);
                _bufferDataLength = length;

                try
                {
                    NewPackageRecv();
                }
                catch (Exception ex)
                {
                    OnException(ex);
                    OnFinish();
                }
            }
            else
            {
                // start reading
                socket.BeginReceive(_lineBuffer, 0, maxLineBytes, 0, ReceiveCallback, 0);
            }
        }

        public LineReader(WrappedSocket socket, Func<string, object, bool> onLineRead,
            Action<Exception, object> onException,
            Action<byte[], int, int, object> onFinish, Encoding encoding, string delimiter, int maxLineBytes,
            object state)
            : this(socket, null, 0, 0, onLineRead, onException, onFinish, encoding, delimiter, maxLineBytes, state)
        {
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var bytesRead = _socket.EndReceive(ar);

                if (bytesRead == 0)
                {
                    OnFinish();
                    return;
                }

                _bufferDataLength += bytesRead;

                NewPackageRecv();
            }
            catch (Exception ex)
            {
                OnException(ex);
                OnFinish();
            }
        }

        private void NewPackageRecv()
        {
            int i;
            while ((i = IndexOf(_lineBuffer, _bufferDataIndex, _bufferDataLength, _delimiterBytes, _delimiterSearchOffsetTable,
                       _delimiterSearchCharTable)) != -1)
            {
                var decodeLen = i - _bufferDataIndex;
                string line = _encoding.GetString(_lineBuffer, _bufferDataIndex, decodeLen);

                _bufferDataIndex = i + _delimiterBytes.Length;
                _bufferDataLength -= decodeLen;
                _bufferDataLength -= _delimiterBytes.Length;

                var stop = _onLineRead(line, _state);
                if (stop)
                {
                    OnFinish();
                    return;
                }
            }
            if (_bufferDataLength == _lineBuffer.Length)
            {
                OnException(new IndexOutOfRangeException("LineBuffer full! Try increace maxLineBytes!"));
                OnFinish();

                return;
            }

            if (_bufferDataIndex > 0)
            {
                Buffer.BlockCopy(_lineBuffer, _bufferDataIndex, _lineBuffer, 0, _bufferDataLength);
                _bufferDataIndex = 0;
            }

            _socket.BeginReceive(_lineBuffer, _bufferDataLength, _lineBuffer.Length - _bufferDataLength, 0, ReceiveCallback, _bufferDataLength);
        }

        private void OnException(Exception ex)
        {
            _onException?.Invoke(ex, _state);
        }

        private void OnFinish()
        {
            _onFinish?.Invoke(_lineBuffer, _bufferDataIndex, _bufferDataLength, _state);
        }

        #region Boyer-Moore string search

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

        #endregion
    }
}
