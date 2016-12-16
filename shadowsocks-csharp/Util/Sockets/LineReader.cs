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
        private readonly ByteSearch.SearchTarget _delimiterSearch;
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

            _delimiterSearch = new ByteSearch.SearchTarget(_delimiterBytes);

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
            while ((i = _delimiterSearch.SearchIn(_lineBuffer, _bufferDataIndex, _bufferDataLength)) != -1)
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
    }
}
