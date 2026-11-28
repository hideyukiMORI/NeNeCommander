using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class WindowsOverwriteCompletionFault
{
    private readonly string _defect;
    private bool _active;
    private long _initialLength;

    internal WindowsOverwriteCompletionFault(string defect)
    {
        _defect = defect;
        Io = new WindowsTransferContentIo(ReadAsync, WriteAsync, SetLength, Flush);
    }

    internal WindowsTransferContentIo Io { get; }
    internal int Writes { get; private set; }
    internal int Reads { get; private set; }
    internal int Flushes { get; private set; }

    internal void Arm(RecoveryFrameEvidence initial)
    {
        _initialLength = initial.Size.Bytes;
        _active = true;
    }

    private async ValueTask<int> ReadAsync(SafeFileHandle handle, Memory<byte> bytes, long offset, CancellationToken token)
    {
        Assert.IsLessThanOrEqualTo(65536, bytes.Length);
        if (_active)
        {
            Reads++;
        }
        if (_active && Writes > 0 && _defect == "readThrow")
        {
            throw new IOException("Injected completion read failure.");
        }
        int count = await RandomAccess.ReadAsync(handle, bytes[..Math.Min(4093, bytes.Length)], offset, token);
        if (_active && Writes > 0 && _defect == "readMismatch" && count > 0)
        {
            bytes.Span[0] = 0;
        }
        return count;
    }

    private async ValueTask WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> bytes, long offset, CancellationToken token)
    {
        Assert.IsLessThanOrEqualTo(65536, bytes.Length);
        if (!_active)
        {
            await RandomAccess.WriteAsync(handle, bytes, offset, token);
            return;
        }
        Assert.IsTrue(offset >= _initialLength);
        Writes++;
        if (_defect == "before")
        {
            throw new IOException("Injected failure before append bytes.");
        }
        int length = _defect == "partial" ? Math.Min(7, bytes.Length) : bytes.Length;
        await RandomAccess.WriteAsync(handle, bytes[..length], offset, token);
        if (_defect is "partial" or "after")
        {
            throw new IOException("Injected failure after append bytes.");
        }
        if (_defect == "extra")
        {
            byte[] extra = [42];
            await RandomAccess.WriteAsync(handle, extra, offset + bytes.Length, token);
        }
    }

    private void SetLength(SafeFileHandle handle, long length)
    {
        Assert.IsFalse(_active, "Completion must never truncate the initial mapping.");
        RandomAccess.SetLength(handle, length);
    }

    private void Flush(SafeFileHandle handle)
    {
        if (_active)
        {
            Flushes++;
            if (_defect == "flush")
            {
                throw new IOException("Injected completion flush failure.");
            }
        }
        RandomAccess.FlushToDisk(handle);
    }
}
