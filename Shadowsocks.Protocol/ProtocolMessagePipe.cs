using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Protocol
{
    public class ProtocolMessagePipe
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;

        public ProtocolMessagePipe(IDuplexPipe pipe)
        {
            _reader = pipe.Input;
            _writer = pipe.Output;
        }

        public async Task<T> ReadAsync<T>(int millisecond) where T : IProtocolMessage, new()
        {
            var delay = new CancellationTokenSource();
            delay.CancelAfter(millisecond);

            return await ReadAsync<T>(delay.Token);
        }

        public async Task<T> ReadAsync<T>(T ret, int millisecond) where T : IProtocolMessage
        {
            var delay = new CancellationTokenSource();
            delay.CancelAfter(millisecond);

            return await ReadAsync(ret, delay.Token);
        }

        public async Task<T> ReadAsync<T>(CancellationToken token = default) where T : IProtocolMessage, new() => await ReadAsync(new T(), token);

        public async Task<T> ReadAsync<T>(T ret, CancellationToken token = default) where T : IProtocolMessage
        {
            Debug.WriteLine($"Reading protocol message {typeof(T).Name}");
            //var ret = new T();
            var required = 0;
            do
            {
                var seq = ReadOnlySequence<byte>.Empty;
                var eof = false;
                var ctr = 0;
                do
                {
                    if (eof)
                        throw new FormatException(
                            $"Message {typeof(T)} parse error, required {required} byte, {seq.Length} byte remain");
                    var result = await _reader.ReadAsync(token);
                    seq = result.Buffer;
                    eof = result.IsCompleted;
                    if (seq.Length == 0)
                    {
                        if (++ctr > 1000)
                            throw new FormatException($"Message {typeof(T)} parse error, maybe EOF");
                    }
                } while (seq.Length < required);

                var frame = MakeFrame(seq);
                (var ok, var len) = ret.TryLoad(frame);
                if (ok)
                {
                    var ptr = seq.GetPosition(len, seq.Start);
                    _reader.AdvanceTo(ptr);
                    break;
                }

                if (len == 0)
                {
                    var arr = Util.GetArray(frame).Array;
                    if (arr == null) throw new FormatException($"Message {typeof(T)} parse error");
                    throw new FormatException(
                        $"Message {typeof(T)} parse error, {Environment.NewLine}{BitConverter.ToString(arr)}");
                }

                required = len;
            } while (true);

            return ret;
        }

        public async Task WriteAsync(IProtocolMessage msg, CancellationToken token = default)
        {
            Debug.WriteLine($"Writing protocol message {msg}");

            Memory<byte> buf;
            var estSize = 4096;
            int size;
            do
            {
                buf = _writer.GetMemory(estSize);
                try
                {
                    size = msg.Serialize(buf);
                }
                catch (ArgumentException)
                {
                    estSize *= 2;
                    continue;
                }
                if (estSize > 65536) throw new ArgumentException("Protocol message is too large");
                _writer.Advance(size);
                await _writer.FlushAsync(token);

                return;
            } while (true);
        }

        private SequencePosition _lastFrameStart;
        private SequencePosition _lastFrameEnd;
        private ReadOnlyMemory<byte> _lastFrame;

        public ReadOnlyMemory<byte> MakeFrame(ReadOnlySequence<byte> seq)
        {
            // cached frame
            if (_lastFrameStart.Equals(seq.Start) && _lastFrameEnd.Equals(seq.End))
            {
                Debug.WriteLine("Hit cached frame");
                return _lastFrame;
            }

            _lastFrameStart = seq.Start;
            _lastFrameEnd = seq.End;

            if (seq.IsSingleSegment)
            {
                Debug.WriteLine("Frame is single segement");
                _lastFrame = seq.First;
                return seq.First;
            }

            Debug.WriteLine("Copy frame data into single Memory");
            Memory<byte> ret = new byte[seq.Length];
            var ptr = 0;
            foreach (var mem in seq)
            {
                mem.CopyTo(ret.Slice(ptr));
                ptr += mem.Length;
            }

            _lastFrame = ret;
            return ret;
        }
    }
}