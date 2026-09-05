using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>
/// Reads the direct entries of one Windows local directory without recursion, link following,
/// or silent omission of inaccessible content.
/// </summary>
public sealed class WindowsLocalDirectoryReader : IDirectoryReadPort
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;

    /// <summary>
    /// HRESULT for <c>ERROR_INVALID_PARAMETER</c>, which Windows raises when a directory
    /// enumeration is attempted on a handle that does not refer to a directory.
    /// </summary>
    private const int NotADirectoryHResult = unchecked((int)0x80070057);

    /// <summary>Initializes a reader with the default Windows local I/O execution boundary.</summary>
    public WindowsLocalDirectoryReader()
        : this(new WindowsLocalIoExecutionBoundary())
    {
    }

    /// <summary>Initializes a reader with the composed Windows local I/O execution boundary.</summary>
    /// <param name="executionBoundary">Shared boundary for synchronous Windows filesystem work.</param>
    public WindowsLocalDirectoryReader(WindowsLocalIoExecutionBoundary executionBoundary)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        _executionBoundary = executionBoundary;
    }

    /// <inheritdoc />
    public Task<DirectoryReadOutcome> ReadAsync(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _executionBoundary.ExecuteAsync(() => Read(request, cancellationToken));
    }

    internal static DirectoryReadOutcome TranslateListingCreation(DirectoryListingCreation creation)
    {
        return creation switch
        {
            DirectoryListingAccepted accepted => DirectoryReadOutcome.Succeeded(accepted.Listing),
            DirectoryListingRejected => DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable),
            _ => throw new InvalidOperationException("The listing creation variant is not translatable."),
        };
    }

    private static DirectoryReadOutcome Read(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        if (request.Location is not WindowsLocalPath location)
        {
            return DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return DirectoryReadOutcome.Cancelled();
        }

        try
        {
            return Enumerate(location, request.EntryBoundary, cancellationToken);
        }
        catch (UnauthorizedAccessException exception)
        {
            return DirectoryReadOutcome.Failed(WindowsFileFailureNormalizer.Normalize(exception.HResult));
        }
        catch (IOException exception)
        {
            return DirectoryReadOutcome.Failed(NormalizeEnumerationFailure(exception.HResult));
        }
    }

    private static DirectoryReadOutcome Enumerate(
        WindowsLocalPath location,
        int entryBoundary,
        CancellationToken cancellationToken)
    {
        List<DirectoryEntry> entries = [];
        int reportedEntryCount = 0;
        int unrepresentableEntryCount = 0;
        DirectoryListingCompleteness completeness = DirectoryListingCompleteness.Complete;
        DirectoryInfo directory = new(location.CanonicalText);
        foreach (FileSystemInfo info in directory.EnumerateFileSystemInfos("*", CreateDirectEntryOptions()))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return DirectoryReadOutcome.Cancelled();
            }
            if (reportedEntryCount == entryBoundary)
            {
                completeness = DirectoryListingCompleteness.Bounded;
                break;
            }

            reportedEntryCount++;
            if (FileSystemPath.Parse(BuildChildText(location, info.Name)) is PathParseSuccess child)
            {
                entries.Add(DirectoryEntry.Create(
                    child.Path,
                    info.Name,
                    ClassifyEntry(info),
                    ClassifyVisibility(info)));
            }
            else
            {
                unrepresentableEntryCount++;
            }
        }

        return TranslateListingCreation(
            DirectoryListing.Create(location, entries, completeness, unrepresentableEntryCount));
    }

    internal static string BuildChildText(WindowsLocalPath location, string entryName)
    {
        return location.CanonicalText.EndsWith('\\')
            ? location.CanonicalText + entryName
            : location.CanonicalText + "\\" + entryName;
    }

    /// <summary>
    /// Creates enumeration options that report every attribute class and surface access
    /// failures instead of returning an empty listing for a denied directory. Built per read
    /// so the contract is exercised by every test rather than frozen in a static initializer.
    /// </summary>
    private static EnumerationOptions CreateDirectEntryOptions()
    {
        return new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        };
    }

    private static DirectoryEntryKind ClassifyEntry(FileSystemInfo info)
    {
        return info is DirectoryInfo ? DirectoryEntryKind.Directory : DirectoryEntryKind.File;
    }

    /// <summary>
    /// Reports the visibility Windows itself records for the entry. The attributes come from the
    /// enumeration, so no second query touches the volume, and the entry name never takes part in
    /// the decision: a name beginning with a dot is an ordinary Windows entry.
    /// </summary>
    private static EntryVisibility ClassifyVisibility(FileSystemInfo info)
    {
        return (info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0
            ? EntryVisibility.Normal
            : EntryVisibility.Hidden;
    }

    internal static FileOperationFailureKind NormalizeEnumerationFailure(int hResult)
    {
        return hResult == NotADirectoryHResult
            ? FileOperationFailureKind.NotFound
            : WindowsFileFailureNormalizer.Normalize(hResult);
    }
}
