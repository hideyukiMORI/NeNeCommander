using System;
using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Rechecks the entry and its filesystem boundary before Shell may recycle it.</summary>
internal static class WindowsRecycleAdmission
{
    internal static FileOperationFailureKind? Inspect(WindowsLocalPath path)
    {
        return Inspect(path, new WindowsDriveBoundary());
    }

    internal static FileOperationFailureKind? Inspect(WindowsLocalPath path, WindowsDriveBoundary driveBoundary)
    {
        return path.Parent is not WindowsLocalPath parent || !driveBoundary.AdmitsLocal(path) ||
            driveBoundary.ReadType(path) != DriveType.Fixed
            ? FileOperationFailureKind.ProviderUnavailable
            : WindowsLocalTransferDestination.Failure(parent, driveBoundary) ?? InspectTree(path, driveBoundary);
    }

    internal static FileOperationFailureKind? Revalidate(FileEntrySnapshot source)
    {
        return Revalidate(source, new WindowsDriveBoundary());
    }

    internal static FileOperationFailureKind? Revalidate(FileEntrySnapshot source, WindowsDriveBoundary driveBoundary)
    {
        try
        {
            return source.Path is not WindowsLocalPath local || !driveBoundary.AdmitsLocal(local)
                ? FileOperationFailureKind.ProviderUnavailable
                : WindowsLocalEntryIdentity.Revalidate(source) switch
                {
                    EntryRejected rejected => rejected.Failure,
                    EntryMatched => Inspect(local, driveBoundary),
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

    private static FileOperationFailureKind? InspectTree(WindowsLocalPath path, WindowsDriveBoundary driveBoundary)
    {
        FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(path);
        return entry is null
            ? FileOperationFailureKind.NotFound
            : WindowsLocalTreeCopy.IsReparsePoint(entry) || WindowsLocalTreeCopy.ContainsReparsePoint(
                entry, child => WindowsLocalTreeCopy.InspectLocalSource(child, driveBoundary))
                ? FileOperationFailureKind.ProviderUnavailable
                : null;
    }
}
