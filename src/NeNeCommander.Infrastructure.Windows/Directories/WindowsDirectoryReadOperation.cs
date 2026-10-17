using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Drives;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Directories;

/// <summary>Owns the one bounded direct-enumeration algorithm shared by Windows namespaces.</summary>
internal sealed class WindowsDirectoryReadOperation
{
    private const int NotADirectoryHResult = unchecked((int)0x80070057);
    private readonly Func<IEnumerable<WindowsDirectoryEntrySnapshot>> _enumerate;
    private readonly Func<WindowsDirectoryEntrySnapshot, EntryVisibility> _classifyVisibility;
    private readonly Func<DriveKind?> _observeDriveKind;

    internal WindowsDirectoryReadOperation(
        Func<IEnumerable<WindowsDirectoryEntrySnapshot>> enumerate,
        Func<WindowsDirectoryEntrySnapshot, EntryVisibility> classifyVisibility,
        Func<DriveKind?> observeDriveKind)
    {
        ArgumentNullException.ThrowIfNull(enumerate);
        ArgumentNullException.ThrowIfNull(classifyVisibility);
        ArgumentNullException.ThrowIfNull(observeDriveKind);
        _enumerate = enumerate;
        _classifyVisibility = classifyVisibility;
        _observeDriveKind = observeDriveKind;
    }

    /// <summary>
    /// Reads direct entries through the provider's deferred enumeration. Enumeration starts only
    /// after the cancellation check and inside failure normalization; every entry is projected
    /// back onto the validated location by name, so transport text never reaches a listing.
    /// </summary>
    internal DirectoryReadOutcome Read(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return DirectoryReadOutcome.Cancelled();
        }

        try
        {
            DriveKind? driveKind = _observeDriveKind();
            return cancellationToken.IsCancellationRequested
                ? DirectoryReadOutcome.Cancelled()
                : Enumerate(request, driveKind, cancellationToken);
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

    /// <summary>
    /// Reports the visibility Windows itself records for a Windows local or UNC entry. The
    /// attributes come from the enumeration, so no second query touches the volume or share, and
    /// the entry name never takes part in the decision: a name beginning with a dot is an ordinary
    /// Windows entry.
    /// </summary>
    internal static EntryVisibility ClassifyByAttributes(WindowsDirectoryEntrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return (snapshot.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0
            ? EntryVisibility.Normal
            : EntryVisibility.Hidden;
    }

    internal static DirectoryReadOutcome TranslateListingCreation(DirectoryListingCreation creation, DriveKind? driveKind)
    {
        ArgumentNullException.ThrowIfNull(creation);
        return creation is DirectoryListingAccepted accepted
            ? DirectoryReadOutcome.Succeeded(accepted.Listing, driveKind)
            : DirectoryReadOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
    }

    internal static FileOperationFailureKind NormalizeEnumerationFailure(int hResult)
    {
        return hResult == NotADirectoryHResult
            ? FileOperationFailureKind.NotFound
            : WindowsFileFailureNormalizer.Normalize(hResult);
    }

    private DirectoryReadOutcome Enumerate(
        DirectoryReadRequest request, DriveKind? driveKind, CancellationToken cancellationToken)
    {
        List<DirectoryEntry> entries = [];
        int reportedEntryCount = 0;
        int unrepresentableEntryCount = 0;
        DirectoryListingCompleteness completeness = DirectoryListingCompleteness.Complete;
        foreach (WindowsDirectoryEntrySnapshot snapshot in _enumerate())
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
                    EntryMetadata.Create(_classifyVisibility(snapshot), snapshot.Size, snapshot.Modified)));
            }
            else
            {
                unrepresentableEntryCount++;
            }
        }

        return cancellationToken.IsCancellationRequested
            ? DirectoryReadOutcome.Cancelled()
            : TranslateListingCreation(
                DirectoryListing.Create(
                    request.Location,
                    entries,
                    completeness,
                    unrepresentableEntryCount), driveKind);
    }
}
