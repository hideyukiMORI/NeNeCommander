using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Owns the one bounded direct-enumeration algorithm shared by Windows namespaces.</summary>
internal static class WindowsDirectoryReadOperation
{
    private const int NotADirectoryHResult = unchecked((int)0x80070057);

    internal static DirectoryReadOutcome Read(
        DirectoryReadRequest request,
        IWindowsDirectoryEnumerator enumerator,
        Func<WindowsDirectoryEntrySnapshot, EntryVisibility> classifyVisibility,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(enumerator);
        ArgumentNullException.ThrowIfNull(classifyVisibility);
        if (cancellationToken.IsCancellationRequested)
        {
            return DirectoryReadOutcome.Cancelled();
        }

        try
        {
            return Enumerate(request, enumerator, classifyVisibility, cancellationToken);
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

    internal static DirectoryReadOutcome TranslateListingCreation(DirectoryListingCreation creation)
    {
        ArgumentNullException.ThrowIfNull(creation);
        return creation is DirectoryListingAccepted accepted
            ? DirectoryReadOutcome.Succeeded(accepted.Listing)
            : DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
    }

    internal static FileOperationFailureKind NormalizeEnumerationFailure(int hResult)
    {
        return hResult == NotADirectoryHResult
            ? FileOperationFailureKind.NotFound
            : WindowsFileFailureNormalizer.Normalize(hResult);
    }

    private static DirectoryReadOutcome Enumerate(
        DirectoryReadRequest request,
        IWindowsDirectoryEnumerator enumerator,
        Func<WindowsDirectoryEntrySnapshot, EntryVisibility> classifyVisibility,
        CancellationToken cancellationToken)
    {
        List<DirectoryEntry> entries = [];
        int reportedEntryCount = 0;
        int unrepresentableEntryCount = 0;
        DirectoryListingCompleteness completeness = DirectoryListingCompleteness.Complete;
        foreach (WindowsDirectoryEntrySnapshot snapshot in enumerator.Enumerate(request.Location.CanonicalText))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return DirectoryReadOutcome.Cancelled();
            }
            if (reportedEntryCount == request.EntryBoundary)
            {
                completeness = DirectoryListingCompleteness.Bounded;
                break;
            }

            reportedEntryCount++;
            if (request.Location.Child(snapshot.Name) is PathParseSuccess child)
            {
                entries.Add(DirectoryEntry.Create(
                    child.Path,
                    snapshot.Name,
                    snapshot.Kind,
                    classifyVisibility(snapshot)));
            }
            else
            {
                unrepresentableEntryCount++;
            }
        }

        return TranslateListingCreation(
            DirectoryListing.Create(
                request.Location,
                entries,
                completeness,
                unrepresentableEntryCount));
    }
}
