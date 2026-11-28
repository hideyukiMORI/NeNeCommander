using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Suspends one explicitly selected native read acknowledgement while retaining ordinary native writes and flushes.</summary>
internal sealed class TransferReadSuspension
{
    private readonly Func<SafeFileHandle, long, bool> _select;
    private readonly TaskCompletionSource<int> _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _armed;
    private int _count;

    internal TransferReadSuspension(Func<SafeFileHandle, long, bool> select)
    {
        ArgumentNullException.ThrowIfNull(select);
        _select = select;
        Io = new(ReadAsync, RandomAccess.WriteAsync, RandomAccess.SetLength, RandomAccess.FlushToDisk);
    }

    internal WindowsTransferContentIo Io { get; }
    internal bool Suspended { get; private set; }

    internal void Arm()
    {
        _armed = true;
    }

    internal void Release()
    {
        _ = _released.TrySetResult(_count);
    }

    private ValueTask<int> ReadAsync(SafeFileHandle handle, Memory<byte> bytes, long offset, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        int count = RandomAccess.Read(handle, bytes.Span, offset);
        if (!_armed || !_select(handle, offset))
        {
            return ValueTask.FromResult(count);
        }
        _armed = false;
        _count = count;
        Suspended = true;
        return new ValueTask<int>(_released.Task);
    }
}
