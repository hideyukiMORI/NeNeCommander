using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>
/// Reads the direct entries of one Windows local directory without recursion, link following,
/// or silent omission of inaccessible content.
/// </summary>
public sealed class WindowsLocalDirectoryReader : IDirectoryReadPort
{
    private readonly IWindowsDirectoryEnumerator _enumerator;
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly WindowsDriveBoundary _driveBoundary;

    /// <summary>Initializes a reader with the default Windows local I/O execution boundary.</summary>
    public WindowsLocalDirectoryReader()
        : this(new WindowsLocalIoExecutionBoundary())
    {
    }

    /// <summary>Initializes a reader with the composed Windows local I/O execution boundary.</summary>
    /// <param name="executionBoundary">Shared boundary for synchronous Windows filesystem work.</param>
    public WindowsLocalDirectoryReader(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, new WindowsDirectoryEnumerator())
    {
    }

    internal WindowsLocalDirectoryReader(
        WindowsLocalIoExecutionBoundary executionBoundary,
        IWindowsDirectoryEnumerator enumerator)
        : this(executionBoundary, enumerator, new WindowsDriveBoundary())
    {
    }

    internal WindowsLocalDirectoryReader(
        WindowsLocalIoExecutionBoundary executionBoundary,
        IWindowsDirectoryEnumerator enumerator,
        WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(enumerator);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        _executionBoundary = executionBoundary;
        _enumerator = enumerator;
        _driveBoundary = driveBoundary;
    }

    /// <inheritdoc />
    public Task<DirectoryReadOutcome> ReadAsync(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Location is WindowsLocalPath location
            ? _executionBoundary.ExecuteAsync(
                () => new WindowsDirectoryReadOperation(
                    () => _enumerator.Enumerate(location.CanonicalText),
                    WindowsDirectoryReadOperation.ClassifyByAttributes,
                    () => WindowsDriveCatalog.Classify(_driveBoundary.ReadType(location)))
                    .Read(request, cancellationToken))
            : Task.FromResult(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    internal static DirectoryReadOutcome TranslateListingCreation(DirectoryListingCreation creation)
    {
        return WindowsDirectoryReadOperation.TranslateListingCreation(creation, null);
    }

    internal static FileOperationFailureKind NormalizeEnumerationFailure(int hResult)
    {
        return WindowsDirectoryReadOperation.NormalizeEnumerationFailure(hResult);
    }
}
