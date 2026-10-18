using System;
using System.IO;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Executes local steps inside the adapter's existing I/O boundary with fresh identity and drive admission.</summary>
internal sealed class WindowsLocalOperationSteps
{
    private readonly IWslFileSystem _wslFileSystem;
    private readonly WindowsDriveBoundary _driveBoundary;

    internal WindowsLocalOperationSteps(IWslFileSystem wslFileSystem, WindowsDriveBoundary driveBoundary)
    {
        ArgumentNullException.ThrowIfNull(wslFileSystem);
        ArgumentNullException.ThrowIfNull(driveBoundary);
        _wslFileSystem = wslFileSystem;
        _driveBoundary = driveBoundary;
    }

    internal ProviderStepOutcome Rename(FileEntrySnapshot source, FileSystemPath target)
    {
        return target is not WindowsLocalPath localTarget || !_driveBoundary.AdmitsLocal(localTarget)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(source, _driveBoundary, entry => RenameEntry(entry, source.Path, localTarget));
    }

    internal AtomicMoveCapabilityOutcome AtomicMoveCapability(
        FileEntrySnapshot source,
        FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath localDestination || !_driveBoundary.AdmitsLocal(localDestination))
        {
            return AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            return WindowsLocalFileOperationAdapter.RevalidateLocal(source, _driveBoundary) switch
            {
                EntryRejected rejected => AtomicMoveCapabilityOutcome.Failed(rejected.Failure),
                EntryMatched matched => SupportsAtomicMove(matched.Entry, localDestination),
                _ => throw new InvalidOperationException("The revalidation outcome variant is not executable."),
            };
        }
        catch (UnauthorizedAccessException exception)
        {
            return AtomicMoveCapabilityOutcome.Failed(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
        catch (IOException exception)
        {
            return AtomicMoveCapabilityOutcome.Failed(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
    }

    private static AtomicMoveCapabilityOutcome SupportsAtomicMove(
        FileSystemInfo entry,
        WindowsLocalPath destination)
    {
        DirectoryInfo destinationDirectory = new(destination.CanonicalText);
        return !destinationDirectory.Exists
            ? AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.NotFound)
            : (entry.Attributes & FileAttributes.ReparsePoint) != 0 ||
            (destinationDirectory.Attributes & FileAttributes.ReparsePoint) != 0
            ? AtomicMoveCapabilityOutcome.Unsupported
            : WindowsLocalVolumeIdentity.SharesVolume(entry.FullName, destinationDirectory.FullName)
            ? AtomicMoveCapabilityOutcome.Supported
            : AtomicMoveCapabilityOutcome.Unsupported;
    }

    internal ProviderStepOutcome Move(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination || !_driveBoundary.AdmitsLocal(localDestination)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(source, _driveBoundary, entry => MoveEntry(entry, source, localDestination));
    }

    private ProviderStepOutcome MoveEntry(
        FileSystemInfo entry,
        FileEntrySnapshot source,
        WindowsLocalPath destination)
    {
        DirectoryInfo destinationDirectory = new(destination.CanonicalText);
        if (!destinationDirectory.Exists)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound);
        }
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0 ||
            (destinationDirectory.Attributes & FileAttributes.ReparsePoint) != 0 ||
            !WindowsLocalVolumeIdentity.SharesVolume(entry.FullName, destinationDirectory.FullName))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (ProviderPathContainment.Evaluate(source.Path, destination) is ContainedPath)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (BuildTarget(source, destination, entry) is not WindowsLocalPath target)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        string targetText = target.CanonicalText;
        if (WindowsLocalTransferDestination.Exists(targetText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (source.Path is not WindowsLocalPath localSource || !_driveBoundary.AdmitsLocal(localSource) ||
            !_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (entry is DirectoryInfo)
        {
            Directory.Move(entry.FullName, targetText);
        }
        else
        {
            File.Move(entry.FullName, targetText);
        }
        return ProviderStepOutcome.Succeeded();
    }

    private ProviderStepOutcome RenameEntry(FileSystemInfo entry, FileSystemPath source, WindowsLocalPath target)
    {
        if (!SharesParent(source, target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Inspection);
        }
        if (WindowsLocalTransferDestination.Exists(target.CanonicalText) && !IsSameEntryText(source, target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (source is not WindowsLocalPath localSource || !_driveBoundary.AdmitsLocal(localSource) ||
            !_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (entry is DirectoryInfo)
        {
            Directory.Move(entry.FullName, target.CanonicalText);
            return ProviderStepOutcome.Succeeded();
        }
        File.Move(entry.FullName, target.CanonicalText);
        return ProviderStepOutcome.Succeeded();
    }

    /// <summary>
    /// Confirms the target is a direct child of the source's own parent, so a rename can never
    /// leave that parent. Both parents are derived by the domain path model and compared with the
    /// provider-aware filesystem identity comparer; no string prefix comparison is used.
    /// </summary>
    private static bool SharesParent(FileSystemPath source, WindowsLocalPath target)
    {
        return source.Parent is FileSystemPath sourceParent &&
            target.Parent is FileSystemPath targetParent &&
            FileSystemPathIdentityComparer.Instance.Equals(sourceParent, targetParent);
    }

    /// <summary>
    /// Reports whether the existing target is the source itself, which is how a rename that only
    /// changes letter case reaches the provider. Windows local text is case-insensitive.
    /// </summary>
    private static bool IsSameEntryText(FileSystemPath source, WindowsLocalPath target)
    {
        return target.CanonicalText.Equals(source.CanonicalText, StringComparison.OrdinalIgnoreCase);
    }

    internal ProviderStepOutcome CreateDirectory(FileEntrySnapshot location, FileSystemPath target)
    {
        return target is not WindowsLocalPath localTarget || !_driveBoundary.AdmitsLocal(localTarget)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(location, _driveBoundary, entry => CreateDirectoryBeneath(entry, location.Path, localTarget));
    }

    private ProviderStepOutcome CreateDirectoryBeneath(FileSystemInfo entry, FileSystemPath location, WindowsLocalPath target)
    {
        if (entry is not DirectoryInfo)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.NotFound);
        }
        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (ProviderPathContainment.Evaluate(location, target) is not ContainedPath)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Inspection);
        }
        if (WindowsLocalTransferDestination.Exists(target.CanonicalText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (location is not WindowsLocalPath localLocation || !_driveBoundary.AdmitsLocal(localLocation) ||
            !_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        _ = Directory.CreateDirectory(target.CanonicalText);
        return ProviderStepOutcome.Succeeded();
    }

    internal ProviderStepOutcome Copy(FileEntrySnapshot source, FileSystemPath destination)
    {
        return destination is not WindowsLocalPath localDestination || !_driveBoundary.AdmitsLocal(localDestination)
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(source, _driveBoundary, entry =>
                BuildTarget(source, localDestination, entry) is WindowsLocalPath target
                    ? CopyEntry(entry, target)
                    : ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    private ProviderStepOutcome CopyEntry(FileSystemInfo entry, WindowsLocalPath target)
    {
        if (!_driveBoundary.AdmitsLocal(target))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        string targetText = target.CanonicalText;
        if (WindowsLocalTransferDestination.Exists(targetText))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict);
        }
        if (WindowsLocalTreeCopy.ContainsReparsePoint(entry,
            source => WindowsLocalTreeCopy.InspectLocalSource(source, _driveBoundary)))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            return WindowsLocalTreeCopy.Copy(
                entry, target, new WindowsTreeCopyAccess(
                    path => path.CanonicalText,
                    source => WindowsLocalTreeCopy.InspectLocalSource(source, _driveBoundary),
                    _driveBoundary));
        }
        catch (UnauthorizedAccessException exception)
        {
            return FailedCopy(targetText, WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
        catch (IOException exception)
        {
            return FailedCopy(targetText, WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Copy));
        }
    }

    private static ProviderStepOutcome FailedCopy(string targetText, FileOperationFailureKind failure)
    {
        return WindowsLocalTransferDestination.Exists(targetText)
            ? ProviderStepOutcome.FailedAfterEffect(failure, ProviderStepEffectKind.CopyTargetCreated)
            : ProviderStepOutcome.Failed(failure);
    }

    internal CopyVerificationOutcome Verify(FileEntrySnapshot source, FileSystemPath destination)
    {
        if (destination is not WindowsLocalPath local || !_driveBoundary.AdmitsLocal(local) ||
            WindowsLocalTransferDestination.Failure(local, _driveBoundary) is not null)
        {
            return CopyVerificationOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        RevalidationOutcome revalidation = WindowsLocalFileOperationAdapter.RevalidateTransferSource(source, _driveBoundary);
        if (revalidation is EntryRejected rejected)
        {
            return CopyVerificationOutcome.Failed(rejected.Failure);
        }
        FileSystemInfo entry = ((EntryMatched)revalidation).Entry;
        return BuildTarget(source, local, entry) is WindowsLocalPath target && WindowsLocalTransferDestination.Exists(target.CanonicalText)
            ? _wslFileSystem.VerifyCopy(source, target)
            : CopyVerificationOutcome.Failed(FileOperationFailureKind.Verification);
    }

    internal ProviderStepOutcome Delete(FileEntrySnapshot source, DeletionExecutionMode mode)
    {
        return mode is DeletionExecutionMode.VerifiedCopyMode verified
            ? source.Path is WindowsLocalPath
                ? _wslFileSystem.DeleteVerifiedSource(source, verified.Receipt)
                : ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : mode != DeletionExecutionMode.Permanent
            ? ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalFileOperationAdapter.WithRevalidatedEntry(source, _driveBoundary, entry => DeleteEntry(entry, (WindowsLocalPath)source.Path));
    }

    private ProviderStepOutcome DeleteEntry(FileSystemInfo entry, WindowsLocalPath source)
    {
        if (!_driveBoundary.AdmitsLocal(source))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        if (entry is DirectoryInfo directory)
        {
            directory.Delete(recursive: true);
        }
        else
        {
            entry.Delete();
        }
        return ProviderStepOutcome.Succeeded();
    }

    private static WindowsLocalPath? BuildTarget(
        FileEntrySnapshot source,
        WindowsLocalPath destination,
        FileSystemInfo entry)
    {
        return source.TransferTarget as WindowsLocalPath ??
            WindowsLocalTreeCopy.DeriveChild(destination, entry.Name) as WindowsLocalPath;
    }
}
