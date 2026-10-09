using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>
/// Reads the direct entries of one UNC share directory. The current user's Windows logon session
/// authenticates the request; the reader adds no credential handling, retry, or fallback.
/// </summary>
internal sealed class WindowsUncDirectoryReader : IDirectoryReadPort
{
    private readonly IWindowsDirectoryEnumerator _enumerator;
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;

    internal WindowsUncDirectoryReader(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, new WindowsDirectoryEnumerator())
    {
    }

    internal WindowsUncDirectoryReader(
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
        return request.Location is WindowsUncPath
            ? _executionBoundary.ExecuteAsync(
                () => WindowsDirectoryReadOperation.Read(
                    request,
                    _enumerator,
                    WindowsDirectoryReadOperation.ClassifyByAttributes,
                    cancellationToken))
            : Task.FromResult(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }
}
