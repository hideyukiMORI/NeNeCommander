using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
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
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(enumerator);
        _executionBoundary = executionBoundary;
        _enumerator = enumerator;
    }

    /// <inheritdoc />
    public Task<DirectoryReadOutcome> ReadAsync(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Location is WindowsLocalPath
            ? _executionBoundary.ExecuteAsync(
                () => WindowsDirectoryReadOperation.Read(request, _enumerator, ClassifyVisibility, cancellationToken))
            : Task.FromResult(DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    internal static DirectoryReadOutcome TranslateListingCreation(DirectoryListingCreation creation)
    {
        return WindowsDirectoryReadOperation.TranslateListingCreation(creation);
    }

    /// <summary>
    /// Reports the visibility Windows itself records for the entry. The attributes come from the
    /// enumeration, so no second query touches the volume, and the entry name never takes part in
    /// the decision: a name beginning with a dot is an ordinary Windows entry.
    /// </summary>
    private static EntryVisibility ClassifyVisibility(WindowsDirectoryEntrySnapshot snapshot)
    {
        return (snapshot.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0
            ? EntryVisibility.Normal
            : EntryVisibility.Hidden;
    }

    internal static FileOperationFailureKind NormalizeEnumerationFailure(int hResult)
    {
        return WindowsDirectoryReadOperation.NormalizeEnumerationFailure(hResult);
    }
}
