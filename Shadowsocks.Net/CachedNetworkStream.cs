using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shadowsocks.Net;

// cache first packet for duty-chain pattern listener
public class CachedNetworkStream : Stream
{
    // 256 byte first packet buffer should enough for 99.999...% situation
    // socks5: 0x05 0x....
    // http-pac: GET /pac HTTP/1.1
    // http-proxy: /[a-z]+ .+ HTTP\/1\.[01]/i

    public const int MaxCache = 256;

    public Socket Socket { get; private set; }

    private readonly Stream _stream;

    private readonly byte[] _cache = new byte[MaxCache];
    private long _cachePtr = 0;

    private long _readPtr = 0;

    public CachedNetworkStream(Socket socket)
    {
        _stream = new NetworkStream(socket);
        Socket = socket;
    }

    /// <summary>
    /// Only for test purpose
    /// </summary>
    /// <param name="stream"></param>
    public CachedNetworkStream(Stream stream)
    {
        _stream = stream;
    }

    public override bool CanRead => _stream.CanRead;

    // we haven't run out of cache
    public override bool CanSeek => _cachePtr == _readPtr;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position { get => _readPtr; set => Seek(value, SeekOrigin.Begin); }

    public override void Flush() => _stream.Flush();

    //public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    //{
    //    var endPtr = buffer.Length + readPtr;               // expected ptr after operation
    //    var uncachedLen = Math.Max(endPtr - cachePtr, 0);   // how many data from socket
    //    var cachedLen = buffer.Length - uncachedLen;        // how many data from cache
    //    var emptyCacheLen = MaxCache - cachePtr;            // how many cache remain

    //    int readLen = 0;
    //    var cachedMem = buffer[..(int)cachedLen];
    //    var uncachedMem = buffer[(int)cachedLen..];
    //    if (cachedLen > 0)
    //    {
    //        cache[(int)readPtr..(int)(readPtr + cachedLen)].CopyTo(cachedMem);

    //        readPtr += cachedLen;
    //        readLen += (int)cachedLen;
    //    }
    //    if (uncachedLen > 0)
    //    {
    //        int readStreamLen = await s.ReadAsync(cachedMem, cancellationToken);

    //        int lengthToCache = (int)Math.Min(emptyCacheLen, readStreamLen); // how many data need to cache
    //        if (lengthToCache > 0)
    //        {
    //            uncachedMem[0..lengthToCache].CopyTo(cache[(int)cachePtr..]);
    //            cachePtr += lengthToCache;
    //        }

    //        readPtr += readStreamLen;
    //        readLen += readStreamLen;
    //    }
    //    return readLen;
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
    {
        Span<byte> span = buffer.AsSpan(offset, count);
        return Read(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override int Read(Span<byte> buffer)
    {
        // how many data from socket

        // r: readPtr, c: cachePtr, e: endPtr
        // ptr    0   r   c   e 
        // cached ####+++++
        // read            ++++

        // ptr    0   c   r   e 
        // cached #####
        // read           +++++

        var endPtr = buffer.Length + _readPtr;               // expected ptr after operation
        var uncachedLen = Math.Max(endPtr - Math.Max(_cachePtr, _readPtr), 0);
        var cachedLen = buffer.Length - uncachedLen;        // how many data from cache
        var emptyCacheLen = MaxCache - _cachePtr;            // how many cache remain

        var readLen = 0;

        var cachedSpan = buffer[..(int)cachedLen];
        var uncachedSpan = buffer[(int)cachedLen..];
        if (cachedLen > 0)
        {
            _cache[(int)_readPtr..(int)(_readPtr + cachedLen)].CopyTo(cachedSpan);

            _readPtr += cachedLen;
            readLen += (int)cachedLen;
        }
        if (uncachedLen > 0)
        {
            var readStreamLen = _stream.Read(uncachedSpan);

            // how many data need to cache
            var lengthToCache = (int)Math.Min(emptyCacheLen, readStreamLen);
            if (lengthToCache > 0)
            {
                uncachedSpan[..lengthToCache].ToArray().CopyTo(_cache, _cachePtr);
                _cachePtr += lengthToCache;
            }

            _readPtr += readStreamLen;
            readLen += readStreamLen;
        }
        return readLen;
    }

    /// <summary>
    /// Read first block, will never read into non-cache range
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public int ReadFirstBlock(Span<byte> buffer)
    {
        Seek(0, SeekOrigin.Begin);
        var len = Math.Min(MaxCache, buffer.Length);
        return Read(buffer[0..len]);
    }

    /// <summary>
    /// Seek position, only support seek to cached range when we haven't read into non-cache range
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="origin">Set it to System.IO.SeekOrigin.Begin, otherwise it will throw System.NotSupportedException</param>
    /// <exception cref="IOException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <returns></returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (!CanSeek) throw new NotSupportedException("Non cache data has been read");
        if (origin != SeekOrigin.Begin) throw new NotSupportedException("We don't know network stream's length");
        if (offset < 0 || offset > _cachePtr) throw new NotSupportedException("Can't seek to uncached position");

        _readPtr = offset;
        return Position;
    }

    /// <summary>
    /// Useless
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="NotSupportedException"></exception>
    public override void SetLength(long value) => _stream.SetLength(value);

    /// <summary>
    /// Write to underly stream
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    => _stream.WriteAsync(buffer, offset, count, cancellationToken);

    /// <summary>
    /// Write to underly stream
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        _stream.Dispose();
        base.Dispose(disposing);
    }
}