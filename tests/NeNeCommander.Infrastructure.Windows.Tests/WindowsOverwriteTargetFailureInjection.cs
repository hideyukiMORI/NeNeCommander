using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Injects one target-specific native failure while forwarding all preparation I/O and borrowing the fixture lifetime.</summary>
internal sealed class WindowsOverwriteTargetFailureInjection
{
    private readonly WindowsOverwritePreparationFixture _fixture;
    private readonly string _failure;
    private readonly Func<WindowsOverwriteAttempt?> _attempt;

    internal WindowsOverwriteTargetFailureInjection(WindowsOverwritePreparationFixture fixture, string failure, Func<WindowsOverwriteAttempt?> attempt)
    {
        _fixture = fixture;
        _failure = failure;
        _attempt = attempt;
        Content = new WindowsTransferContentIo(RandomAccess.ReadAsync, WriteAsync, SetLength, RandomAccess.FlushToDisk);
    }

    internal WindowsTransferContentIo Content { get; }

    private async ValueTask WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> bytes, long offset, CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(handle, _fixture.Pair.Target.Handle) || _failure == "truncate")
        {
            await RandomAccess.WriteAsync(handle, bytes, offset, cancellationToken);
            return;
        }
        _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(_attempt()?.Receipt.Target);
        if (_failure != "before")
        {
            await RandomAccess.WriteAsync(handle, _failure == "partial" ? bytes[..1] : bytes, offset, cancellationToken);
        }
        throw new IOException("Injected target write exception.");
    }

    private void SetLength(SafeFileHandle handle, long length)
    {
        if (ReferenceEquals(handle, _fixture.Pair.Target.Handle))
        {
            _ = Assert.IsInstanceOfType<TargetMayHaveChanged>(_attempt()?.Receipt.Target);
        }
        RandomAccess.SetLength(handle, length);
        if (ReferenceEquals(handle, _fixture.Pair.Target.Handle) && _failure == "truncate")
        {
            throw new IOException("Injected target truncation exception.");
        }
    }
}
