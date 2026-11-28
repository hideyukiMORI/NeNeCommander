using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Copies an admitted exact byte range into the supplied held destination; the enclosing owner records possible effects and separately verifies content and identity.</summary>
internal static class WindowsTransferContentCopy
{
    internal static async Task CopyAsync(WindowsTransferContentPair pair, WindowsTransferContentIo io, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pair);
        ArgumentNullException.ThrowIfNull(io);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] buffer = new byte[WindowsTransferContentIo.BufferSize];
        long offset = 0;
        while (offset < pair.Size.Bytes)
        {
            int count = (int)Math.Min(buffer.Length, pair.Size.Bytes - offset);
            await io.ReadExactlyAsync(pair.Source, buffer.AsMemory(0, count), offset, cancellationToken).ConfigureAwait(false);
            await io.WriteAsync(pair.Target, buffer.AsMemory(0, count), offset, cancellationToken).ConfigureAwait(false);
            offset += count;
        }
        await io.RequireEndAsync(pair.Source, offset, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        io.SetLength(pair.Target, pair.Size.Bytes);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
