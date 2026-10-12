using System;
using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Rechecks the entry and its filesystem boundary before Shell may recycle it.</summary>
internal static class WindowsRecycleAdmission
{
    internal static FileOperationFailureKind? Inspect(WindowsLocalPath path)
    {
        return path.Parent is not WindowsLocalPath parent || new DriveInfo(path.Drive).DriveType != DriveType.Fixed
            ? FileOperationFailureKind.ProviderUnavailable
            : WindowsLocalTransferDestination.Failure(parent) ?? InspectTree(path);
    }

    internal static FileOperationFailureKind? Revalidate(FileEntrySnapshot source)
    {
        try
        {
            return WindowsLocalEntryIdentity.Revalidate(source) switch
            {
                EntryRejected rejected => rejected.Failure,
                EntryMatched => Inspect((WindowsLocalPath)source.Path),
                _ => throw new InvalidOperationException("The revalidation variant is not executable."),
            };
        }
        catch (UnauthorizedAccessException)
        {
            return FileOperationFailureKind.AccessDenied;
        }
        catch (IOException exception)
        {
            return WindowsFileFailureNormalizer.Normalize(exception.HResult);
        }
    }

    private static FileOperationFailureKind? InspectTree(WindowsLocalPath path)
    {
        FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(path);
        return entry is null
            ? FileOperationFailureKind.NotFound
            : WindowsLocalTreeCopy.IsReparsePoint(entry) || WindowsLocalTreeCopy.ContainsReparsePoint(entry)
                ? FileOperationFailureKind.ProviderUnavailable
                : null;
    }
}
