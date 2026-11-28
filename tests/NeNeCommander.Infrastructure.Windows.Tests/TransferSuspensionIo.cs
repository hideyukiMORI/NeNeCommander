using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Performs native read/write/truncate effects, suspends one selected acknowledgement and explicitly delegates synchronous flush acknowledgement.</summary>
internal sealed class TransferSuspensionIo
{
    private readonly TaskCompletionSource<int> _read = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Action<SafeFileHandle> _flush;
    private int _suspendAt;
    private int _readCount;

    internal TransferSuspensionIo(Action<SafeFileHandle> flush)
    {
        ArgumentNullException.ThrowIfNull(flush);
        _flush = flush;
        Io = new(ReadAsync, WriteAsync, RandomAccess.SetLength, Flush);
    }

    internal WindowsTransferContentIo Io { get; }
    internal int Calls { get; private set; }
    internal int Flushes { get; private set; }
    internal bool Suspended { get; private set; }

    internal void Arm(int suspendAt)
    {
        Calls = 0;
        _suspendAt = suspendAt;
    }

    internal void Release()
    {
        _ = _read.TrySetResult(_readCount);
        _ = _write.TrySetResult();
    }

    private void Flush(SafeFileHandle handle)
    {
        Assert.IsFalse(handle.IsClosed);
        Assert.IsFalse(handle.IsInvalid);
        _flush(handle);
        Flushes++;
    }

    private ValueTask<int> ReadAsync(SafeFileHandle handle, Memory<byte> bytes, long offset, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        int count = RandomAccess.Read(handle, bytes.Span, offset);
        Calls++;
        if (Calls != _suspendAt)
        {
            return ValueTask.FromResult(count);
        }
        Suspended = true;
        _readCount = count;
        return new ValueTask<int>(_read.Task);
    }

    private ValueTask WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> bytes, long offset, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        RandomAccess.Write(handle, bytes.Span, offset);
        Calls++;
        if (Calls != _suspendAt)
        {
            return ValueTask.CompletedTask;
        }
        Suspended = true;
        return new ValueTask(_write.Task);
    }
}
