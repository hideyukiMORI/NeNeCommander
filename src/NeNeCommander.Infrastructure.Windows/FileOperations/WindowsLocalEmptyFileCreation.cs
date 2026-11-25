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
            ? ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.ProviderUnavailable), ProviderStepReceipt.Empty)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(location, driveBoundary,
                entry => CreateBeneath(entry, location, localTarget, driveBoundary));
    }

    private static ProviderStepOutcome CreateBeneath(
        FileSystemInfo entry, FileEntrySnapshot location, WindowsLocalPath target, WindowsDriveBoundary driveBoundary)
    {
        if (entry is not DirectoryInfo)
        {
            return ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.NotFound), ProviderStepReceipt.Empty);
        }
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            return ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.ProviderUnavailable), ProviderStepReceipt.Empty);
        }
        if (target.Parent is not FileSystemPath parent ||
            !FileSystemPathIdentityComparer.Instance.Equals(location.Path, parent))
        {
            return ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Inspection), ProviderStepReceipt.Empty);
        }
        FileOperationFailureKind? failure = WindowsLocalTransferDestination.AncestorFailure((WindowsLocalPath)location.Path);
        return failure is not null
            ? ProviderStepOutcome.Create(ProviderStepCompletion.Failed(failure), ProviderStepReceipt.Empty)
            : WindowsLocalTransferDestination.Exists(target)
            ? ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Conflict), ProviderStepReceipt.Empty)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(location, driveBoundary,
                _ => WindowsEmptyFileCreation.Create(@"\\?\" + target.CanonicalText));
    }
}
