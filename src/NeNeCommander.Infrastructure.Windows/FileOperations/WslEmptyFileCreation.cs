using System.IO;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Admits one empty WSL child through the existing 9P namespace owner.</summary>
internal static class WslEmptyFileCreation
{
    internal static ProviderStepOutcome Execute(
        IWslFileSystem fileSystem, FileEntrySnapshot location, FileSystemPath target)
    {
        return target is not WslPath wslTarget
            ? ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.ProviderUnavailable), ProviderStepReceipt.Empty)
            : WslFileOperationAdapter.WithRevalidatedEntry(fileSystem, location,
                entry => CreateBeneath(fileSystem, entry, location, wslTarget));
    }

    private static ProviderStepOutcome CreateBeneath(
        IWslFileSystem fileSystem, WslFileSystemEntry entry, FileEntrySnapshot location, WslPath target)
    {
        if (entry.Kind != DirectoryEntryKind.Directory)
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
        FileOperationFailureKind? failure = AncestorFailure(fileSystem, (WslPath)location.Path);
        return failure is not null
            ? ProviderStepOutcome.Create(ProviderStepCompletion.Failed(failure), ProviderStepReceipt.Empty)
            : fileSystem.TargetExists(target)
            ? ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Conflict), ProviderStepReceipt.Empty)
            : WslFileOperationAdapter.WithRevalidatedEntry(fileSystem, location,
                _ => fileSystem.CreateEmptyFile(target));
    }

    private static FileOperationFailureKind? AncestorFailure(IWslFileSystem fileSystem, WslPath location)
    {
        FileSystemPath? path = location;
        while (path is WslPath current)
        {
            WslFileSystemEntry? entry = fileSystem.Find(current);
            if (entry is null || entry.Kind != DirectoryEntryKind.Directory)
            {
                return FileOperationFailureKind.NotFound;
            }
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return FileOperationFailureKind.ProviderUnavailable;
            }
            path = current.Parent;
        }
        return null;
    }
}
