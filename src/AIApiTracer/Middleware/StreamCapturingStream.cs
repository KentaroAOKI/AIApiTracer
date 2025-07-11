using System.IO.Compression;
using System.IO.Pipelines;
using System.Text;

namespace AIApiTracer.Middleware;

public class StreamCapturingStream : Stream
{
    private readonly Stream _innerStream;
    private readonly MemoryStream _captureStream;
    private readonly long _maxCaptureSize;
    private readonly string? _contentEncoding;
    private bool _disposed = false;

    public StreamCapturingStream(Stream innerStream, string? contentEncoding = null, long maxCaptureSize = 1024 * 1024)
    {
        _innerStream = innerStream;
        _captureStream = new MemoryStream();
        _maxCaptureSize = maxCaptureSize;
        _contentEncoding = contentEncoding?.ToLowerInvariant();
    }

    public MemoryStream CaptureStream => _captureStream;

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _innerStream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        // Write to inner stream immediately
        _innerStream.Write(buffer, offset, count);

        // Capture data up to max size
        if (_captureStream.Length < _maxCaptureSize)
        {
            var captureCount = (int)Math.Min(count, _maxCaptureSize - _captureStream.Length);
            _captureStream.Write(buffer, offset, captureCount);
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // Write to inner stream immediately
        await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);

        // Capture data up to max size
        if (_captureStream.Length < _maxCaptureSize)
        {
            var captureCount = (int)Math.Min(count, _maxCaptureSize - _captureStream.Length);
            await _captureStream.WriteAsync(buffer, offset, captureCount, cancellationToken);
        }
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        // Write to inner stream immediately
        await _innerStream.WriteAsync(buffer, cancellationToken);

        // Capture data up to max size
        if (_captureStream.Length < _maxCaptureSize && buffer.Length > 0)
        {
            var captureCount = (int)Math.Min(buffer.Length, _maxCaptureSize - _captureStream.Length);
            await _captureStream.WriteAsync(buffer.Slice(0, captureCount), cancellationToken);
        }
    }

    public string GetCapturedText()
    {
        if (_captureStream.Length == 0)
            return string.Empty;

        _captureStream.Position = 0;
        
        try
        {
            // Decompress if content is encoded
            Stream readStream = _captureStream;
            
            if (!string.IsNullOrEmpty(_contentEncoding))
            {
                switch (_contentEncoding)
                {
                    case "gzip":
                        readStream = new GZipStream(_captureStream, CompressionMode.Decompress, leaveOpen: true);
                        break;
                    case "deflate":
                        readStream = new DeflateStream(_captureStream, CompressionMode.Decompress, leaveOpen: true);
                        break;
                    case "br":
                        readStream = new BrotliStream(_captureStream, CompressionMode.Decompress, leaveOpen: true);
                        break;
                }
            }
            
            using var reader = new StreamReader(readStream, Encoding.UTF8, leaveOpen: true);
            var text = reader.ReadToEnd();
            
            if (_captureStream.Length >= _maxCaptureSize)
            {
                text += "\n\n[Response body truncated at 1MB]";
            }

            return text;
        }
        catch (Exception ex)
        {
            // If decompression fails, return raw data with error message
            _captureStream.Position = 0;
            using var reader = new StreamReader(_captureStream, Encoding.UTF8, leaveOpen: true);
            return $"[Error decompressing response: {ex.Message}]\n\n" + reader.ReadToEnd();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _captureStream.Dispose();
                _innerStream.Dispose();
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _captureStream.DisposeAsync();
            await _innerStream.DisposeAsync();
            _disposed = true;
        }
        await base.DisposeAsync();
    }
}