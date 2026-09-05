using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Reads direct entries from the canonical Windows-side WSL namespace.</summary>
internal sealed class WslDirectoryReader : IDirectoryReadPort
{
    private readonly IWindowsDirectoryEnumerator _enumerator;
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;

    internal WslDirectoryReader(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, new WindowsDirectoryEnumerator())
    {
    }

    internal WslDirectoryReader(
        WindowsLocalIoExecutionBoundary executionBoundary,
        IWindowsDirectoryEnumerator enumerator)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(enumerator);
        _executionBoundary = executionBoundary;
        _enumerator = enumerator;
    }

    public Task<DirectoryReadOutcome> ReadAsync(
        DirectoryReadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Location is WslPath
            ? _executionBoundary.ExecuteAsync(
                () => WindowsDirectoryReadOperation.Read(request, _enumerator, ClassifyVisibility, cancellationToken))
            : Task.FromResult(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    private static EntryVisibility ClassifyVisibility(WindowsDirectoryEntrySnapshot snapshot)
    {
        return snapshot.Name[0] == '.'
            ? EntryVisibility.Hidden
            : EntryVisibility.Normal;
    }
}
