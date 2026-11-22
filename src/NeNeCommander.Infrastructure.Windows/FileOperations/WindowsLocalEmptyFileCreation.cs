using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Admits one empty local child at the existing provider mutation boundary.</summary>
internal static class WindowsLocalEmptyFileCreation
{
    internal static ProviderStepOutcome Execute(
        FileEntrySnapshot location, FileSystemPath target, WindowsDriveBoundary driveBoundary)
    {
        return target is not WindowsLocalPath localTarget || !driveBoundary.AdmitsLocal(localTarget)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(location, driveBoundary,
                entry => CreateBeneath(entry, location, localTarget, driveBoundary));
    }

    private static ProviderStepOutcome CreateBeneath(
        FileSystemInfo entry, FileEntrySnapshot location, WindowsLocalPath target, WindowsDriveBoundary driveBoundary)
    {
        if (entry is not DirectoryInfo)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound);
        }
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (target.Parent is not FileSystemPath parent ||
            !FileSystemPathIdentityComparer.Instance.Equals(location.Path, parent))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Inspection);
        }
        FileOperationFailureKind? failure = WindowsLocalTransferDestination.AncestorFailure((WindowsLocalPath)location.Path);
        return failure is not null
            ? ProviderStepOutcome.Failed(failure)
            : WindowsLocalTransferDestination.Exists(target)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(location, driveBoundary,
                _ => WindowsEmptyFileCreation.Create(@"\\?\" + target.CanonicalText));
    }
}
