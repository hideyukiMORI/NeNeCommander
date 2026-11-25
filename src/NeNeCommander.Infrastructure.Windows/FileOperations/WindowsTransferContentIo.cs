using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Adapts held-handle transfer I/O with bounded reads and cancellation observation; no path opening or ownership inference occurs.</summary>
internal sealed class WindowsTransferContentIo
{
    internal const int BufferSize = 65536;
    private readonly Func<SafeFileHandle, Memory<byte>, long, CancellationToken, ValueTask<int>> _read;
    private readonly Func<SafeFileHandle, ReadOnlyMemory<byte>, long, CancellationToken, ValueTask> _write;

    internal WindowsTransferContentIo(
        Func<SafeFileHandle, Memory<byte>, long, CancellationToken, ValueTask<int>> read,
        Func<SafeFileHandle, ReadOnlyMemory<byte>, long, CancellationToken, ValueTask> write,
        Action<SafeFileHandle, long> setLength,
        Action<SafeFileHandle> flush)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(setLength);
        ArgumentNullException.ThrowIfNull(flush);
        _read = read;
        _write = write;
        SetLength = setLength;
        Flush = flush;
    }

    internal static WindowsTransferContentIo Native { get; } = new(RandomAccess.ReadAsync, RandomAccess.WriteAsync,
        RandomAccess.SetLength, RandomAccess.FlushToDisk);

    // Callers record potentially destructive/flush-uncertain state before these synchronous native calls.
    internal Action<SafeFileHandle, long> SetLength { get; }
    internal Action<SafeFileHandle> Flush { get; }

    internal async ValueTask<int> ReadAsync(SafeFileHandle handle, Memory<byte> buffer, long offset, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(buffer.Length, BufferSize);
        cancellationToken.ThrowIfCancellationRequested();
        int count = await _read(handle, buffer, offset, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return count < 0 || count > buffer.Length
            ? throw new IOException("The transfer read returned an invalid byte count.") : count;
    }

    internal async ValueTask ReadExactlyAsync(SafeFileHandle handle, Memory<byte> buffer, long offset, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int filled = 0;
        while (filled < buffer.Length)
        {
            int count = await ReadAsync(handle, buffer[filled..], checked(offset + filled), cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                throw new IOException("The transfer stream ended before its admitted length.");
            }
            filled += count;
        }
    }

    internal async ValueTask RequireEndAsync(SafeFileHandle handle, long offset, CancellationToken cancellationToken)
    {
        byte[] probe = new byte[1];
        if (await ReadAsync(handle, probe, offset, cancellationToken).ConfigureAwait(false) != 0)
        {
            throw new IOException("The transfer stream exceeds its admitted length.");
        }
    }

    internal async ValueTask WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long offset, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(buffer.Length, BufferSize);
        cancellationToken.ThrowIfCancellationRequested();
        await _write(handle, buffer, offset, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
