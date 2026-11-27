using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Identifiers;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Implements the sole file-operation port for Windows local paths. Every mutation revalidates
/// the preflighted identity first, never follows reparse points, queries guarded recycle capability,
/// and normalizes platform failures to the canonical vocabulary.
/// An absent snapshot is rejected by <see cref="WindowsLocalEntryIdentity.Revalidate"/> before any step runs.
/// </summary>
public sealed class WindowsLocalFileOperationAdapter : IFileOperationPort
{
    private readonly WindowsLocalIoExecutionBoundary _executionBoundary;
    private readonly WindowsRecycleBin _recycleBin;
    private readonly WindowsLocalOperationSteps _steps;
    private readonly WindowsDriveBoundary _driveBoundary;

    /// <summary>Initializes an adapter with explicitly composed operation identifiers and the default I/O boundary.</summary>
    public WindowsLocalFileOperationAdapter(IIdentifierSource identifiers)
        : this(new WindowsLocalIoExecutionBoundary(), identifiers)
    {
    }

    /// <summary>Initializes an adapter over the shared I/O and identifier boundaries.</summary>
    public WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary, IIdentifierSource identifiers)
        : this(executionBoundary, new WindowsDriveBoundary(), identifiers)
    {
    }

    internal WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsDriveBoundary driveBoundary, IIdentifierSource identifiers)
        : this(executionBoundary, new WindowsLocalAdapterDependencies(new WindowsRecycleBin(new WindowsRecycleShell(), driveBoundary),
            new WindowsWslFileSystem(driveBoundary), driveBoundary, WindowsOverwriteDependencies.Native(identifiers)))
    {
    }

    internal WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsRecycleBin recycleBin, IIdentifierSource identifiers)
        : this(executionBoundary, recycleBin, new WindowsDriveBoundary(), identifiers)
    {
    }

    private WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsRecycleBin recycleBin, WindowsDriveBoundary driveBoundary, IIdentifierSource identifiers)
        : this(executionBoundary, new WindowsLocalAdapterDependencies(recycleBin, new WindowsWslFileSystem(driveBoundary),
            driveBoundary, WindowsOverwriteDependencies.Native(identifiers)))
    {
    }

    internal WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary, WindowsLocalAdapterDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(executionBoundary);
        ArgumentNullException.ThrowIfNull(dependencies);
        _executionBoundary = executionBoundary;
        _recycleBin = dependencies.RecycleBin;
        _steps = new WindowsLocalOperationSteps(dependencies.FileSystem, dependencies.Drives, dependencies.Overwrite);
        _driveBoundary = dependencies.Drives;
    }

    internal WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary,
        IWslFileSystem wslFileSystem, IIdentifierSource identifiers)
        : this(executionBoundary, wslFileSystem, new WindowsDriveBoundary(), identifiers)
    {
    }

    internal WindowsLocalFileOperationAdapter(WindowsLocalIoExecutionBoundary executionBoundary,
        IWslFileSystem wslFileSystem, WindowsDriveBoundary driveBoundary, IIdentifierSource identifiers)
        : this(executionBoundary, new WindowsLocalAdapterDependencies(new WindowsRecycleBin(new WindowsRecycleShell(), driveBoundary),
            wslFileSystem, driveBoundary, WindowsOverwriteDependencies.Native(identifiers)))
    {
    }

    /// <inheritdoc />
    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, FileInspectionPurpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(purpose);
        return purpose == FileInspectionPurpose.Deletion
            ? _executionBoundary.ExecuteShellAsync(() => Inspect(path, purpose))
            : _executionBoundary.ExecuteAsync(() => Inspect(path, purpose));
    }

    /// <inheritdoc />
    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => GuardedPreflight(() => WindowsLocalTransferPreflight.Inspect(sources, destination, _driveBoundary)));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(() => _steps.CopyAsync(source, destination, cancellationToken)).Unwrap();
    }

    /// <inheritdoc />
    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(() => _steps.AtomicMoveCapability(source, destination));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => _steps.Move(source, destination), FileOperationFailureKind.Move));
    }

    /// <inheritdoc />
    public Task<CopyVerificationOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return _executionBoundary.ExecuteAsync(
            () => WslFileOperationAdapter.GuardedVerification(() => _steps.Verify(source, destination)));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mode);
        return !source.DeletionCapability.IsInspected && mode is not DeletionExecutionMode.VerifiedCopyMode
            ? Task.FromResult(ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.Inspection), ProviderStepReceipt.Empty))
            : ScheduleDeletionAsync(source, mode, cancellationToken);
    }

    private Task<ProviderStepOutcome> ScheduleDeletionAsync(
        FileEntrySnapshot source, DeletionExecutionMode mode, CancellationToken cancellationToken)
    {
        return mode == DeletionExecutionMode.Recycle
            ? _executionBoundary.ExecuteShellAsync(() => Recycle(source, cancellationToken))
            : _executionBoundary.ExecuteAsync(
                () => Guarded(() => _steps.Delete(source, mode), FileOperationFailureKind.Delete));
    }

    private ProviderStepOutcome Recycle(FileEntrySnapshot source, CancellationToken cancellationToken)
    {
        return source.Path is WindowsLocalPath local && _driveBoundary.AdmitsLocal(local)
            ? _recycleBin.Recycle(source, cancellationToken)
            : ProviderStepOutcome.Create(ProviderStepCompletion.Failed(FileOperationFailureKind.ProviderUnavailable), ProviderStepReceipt.Empty);
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(target);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(
                () => _steps.CreateDirectory(location, target),
                FileOperationFailureKind.ProviderUnavailable));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CreateEmptyFileAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(target);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(
                () => WindowsLocalEmptyFileCreation.Execute(location, target, _driveBoundary),
                FileOperationFailureKind.ProviderUnavailable));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return _executionBoundary.ExecuteAsync(
            () => Guarded(() => _steps.Rename(source, target), FileOperationFailureKind.ProviderUnavailable));
    }

    private FileInspectionOutcome Inspect(FileSystemPath path, FileInspectionPurpose purpose)
    {
        if (path is not WindowsLocalPath local || !_driveBoundary.AdmitsLocal(local))
        {
            return FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        try
        {
            FileSystemInfo? entry = WindowsLocalEntryIdentity.Find(local);
            return entry is null
                ? FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound)
                : FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(
                    path,
                    WindowsLocalEntryIdentity.Describe(entry),
                    purpose == FileInspectionPurpose.Deletion
                        ? _recycleBin.GetCapability(local)
                        : DeletionCapability.NotRequested));
        }
        catch (UnauthorizedAccessException exception)
        {
            return FileInspectionOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
        catch (IOException exception)
        {
            return FileInspectionOutcome.Failed(Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
    }

    /// <summary>
    /// Revalidates one transfer source and rejects it when the entry or any entry beneath it is a
    /// reparse point. Every transfer preflight that reads a Windows local source uses this check,
    /// whatever the destination provider (ADR-0059).
    /// </summary>
    internal static RevalidationOutcome RevalidateTransferSource(FileEntrySnapshot source, WindowsDriveBoundary driveBoundary)
    {
        RevalidationOutcome revalidation = RevalidateLocal(source, driveBoundary);
        return revalidation is EntryMatched matched && WindowsLocalTreeCopy.ContainsReparsePoint(
            matched.Entry, entry => WindowsLocalTreeCopy.InspectLocalSource(entry, driveBoundary))
            ? new EntryRejected(FileOperationFailureKind.ProviderUnavailable)
            : revalidation;
    }

    internal static ProviderStepOutcome WithRevalidatedEntry(
        FileEntrySnapshot source,
        WindowsDriveBoundary driveBoundary,
        Func<FileSystemInfo, ProviderStepOutcome> step)
    {
        return RevalidateLocal(source, driveBoundary) switch
        {
            EntryRejected rejected => ProviderStepOutcome.Create(ProviderStepCompletion.Failed(rejected.Failure), ProviderStepReceipt.Empty),
            EntryMatched matched => step(matched.Entry),
            _ => throw new InvalidOperationException("The revalidation outcome variant is not executable."),
        };
    }

    internal static RevalidationOutcome RevalidateLocal(FileEntrySnapshot source, WindowsDriveBoundary driveBoundary)
    {
        return source.Path is not WindowsLocalPath local || !driveBoundary.AdmitsLocal(local)
            ? new EntryRejected(FileOperationFailureKind.ProviderUnavailable)
            : WindowsLocalEntryIdentity.Revalidate(source);
    }

    internal static ProviderStepOutcome Guarded(Func<ProviderStepOutcome> step, FileOperationFailureKind fallback)
    {
        try
        {
            return step();
        }
        catch (UnauthorizedAccessException exception)
        {
            return ProviderStepOutcome.Create(ProviderStepCompletion.Failed(Normalize(exception.HResult, fallback)), ProviderStepReceipt.Empty);
        }
        catch (IOException exception)
        {
            return ProviderStepOutcome.Create(ProviderStepCompletion.Failed(Normalize(exception.HResult, fallback)), ProviderStepReceipt.Empty);
        }
    }

    internal static FileOperationFailureKind Normalize(int hResult, FileOperationFailureKind fallback)
    {
        FileOperationFailureKind normalized = WindowsFileFailureNormalizer.Normalize(hResult);
        return normalized == FileOperationFailureKind.ProviderUnavailable ? fallback : normalized;
    }

    private static TransferPreflightOutcome GuardedPreflight(Func<TransferPreflightOutcome> step)
    {
        try
        {
            return step();
        }
        catch (UnauthorizedAccessException exception)
        {
            return TransferPreflightOutcome.Rejected(
                Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
        catch (IOException exception)
        {
            return TransferPreflightOutcome.Rejected(
                Normalize(exception.HResult, FileOperationFailureKind.Inspection));
        }
    }

}
