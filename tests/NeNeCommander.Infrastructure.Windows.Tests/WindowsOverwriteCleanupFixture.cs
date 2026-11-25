using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class WindowsOverwriteCleanupFixture
{
    private readonly Dictionary<TransferArtifactRole, SafeFileHandle> _handles = [];

    internal WindowsOverwriteCleanupFixture()
    {
        Io = new((handle, bytes, offset, token) => Read(handle, bytes, offset, token), WriteAsync, SetLength, RandomAccess.FlushToDisk);
    }

    internal WindowsOverwriteAttempt? Attempt { get; set; }
    internal WindowsTransferContentIo Io { get; }
    internal Func<SafeFileHandle, Memory<byte>, long, CancellationToken, ValueTask<int>> Read { get; set; } = RandomAccess.ReadAsync;

    internal SafeFileHandle Handle(TransferArtifactRole role)
    {
        return _handles[role];
    }

    internal static async Task<WindowsTransferRecoveryMapping> CompleteAsync(WindowsOverwriteAttempt attempt)
    {
        WindowsTransferRecoveryMapping mapping = await attempt.PrepareAsync(CancellationToken.None);
        await attempt.OverwritePreparedAsync(CancellationToken.None);
        await attempt.AppendCompletionAsync(CancellationToken.None);
        return mapping;
    }

    private ValueTask WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> bytes, long offset, CancellationToken cancellationToken)
    {
        Capture(handle);
        return RandomAccess.WriteAsync(handle, bytes, offset, cancellationToken);
    }

    private void SetLength(SafeFileHandle handle, long length)
    {
        Capture(handle);
        RandomAccess.SetLength(handle, length);
    }

    private void Capture(SafeFileHandle handle)
    {
        FileObjectIdentity identity = WindowsTransferArtifactQueries.Native.ReadIdentity(handle);
        IdentifiedTransferArtifact? artifact = Attempt?.Receipt.Artifacts.OfType<IdentifiedTransferArtifact>().SingleOrDefault(candidate => candidate.Artifact.ObjectIdentity == identity);
        if (artifact is not null)
        {
            _handles[artifact.Role] = handle;
        }
    }
}
