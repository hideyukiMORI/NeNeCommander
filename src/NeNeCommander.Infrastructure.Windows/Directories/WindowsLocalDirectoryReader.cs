using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>
/// Reads the direct entries of one Windows local directory without recursion, link following,
/// or silent omission of inaccessible content.
/// </summary>
public sealed class WindowsLocalDirectoryReader : IDirectoryReadPort
{
    /// <summary>
    /// HRESULT for <c>ERROR_INVALID_PARAMETER</c>, which Windows raises when a directory
    /// enumeration is attempted on a handle that does not refer to a directory.
    /// </summary>
    private const int NotADirectoryHResult = unchecked((int)0x80070057);

    /// <summary>
    /// Enumeration options that report every attribute class and surface access failures
    /// instead of returning an empty listing for a denied directory.
    /// </summary>
    private static readonly EnumerationOptions DirectEntries = new()
    {
        AttributesToSkip = FileAttributes.None,
        IgnoreInaccessible = false,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

    /// <inheritdoc />
    public Task<DirectoryReadOutcome> ReadAsync(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(Read(request, cancellationToken));
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
        foreach (FileSystemInfo info in directory.EnumerateFileSystemInfos("*", DirectEntries))
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
                entries.Add(DirectoryEntry.Create(child.Path, info.Name, ClassifyEntry(info)));
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

    private static DirectoryEntryKind ClassifyEntry(FileSystemInfo info)
    {
        return info is DirectoryInfo ? DirectoryEntryKind.Directory : DirectoryEntryKind.File;
    }

    private static FileOperationFailureKind NormalizeEnumerationFailure(int hResult)
    {
        return hResult == NotADirectoryHResult
            ? FileOperationFailureKind.NotFound
            : WindowsFileFailureNormalizer.Normalize(hResult);
    }
}
