using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Routes one canonical mutation port to Windows-side provider adapters. Transfer members follow the
/// closed <see cref="TransferRoute"/> of their source and destination pair (ADR-0059); inspection,
/// deletion, directory creation, and rename follow the frozen source provider alone.
/// </summary>
public sealed class ProviderFileOperationPort : IFileOperationPort
{
    private readonly IFileOperationPort _windowsLocal;
    private readonly IFileOperationPort _wsl;
    private readonly IFileOperationPort _windowsLocalToWsl;

    /// <summary>Initializes the provider router over the shared Windows I/O execution boundary.</summary>
    public ProviderFileOperationPort(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(executionBoundary, new WindowsWslFileSystem())
    {
    }

    private ProviderFileOperationPort(
        WindowsLocalIoExecutionBoundary executionBoundary,
        WindowsWslFileSystem wslFileSystem)
        : this(
            new WindowsLocalFileOperationAdapter(executionBoundary),
            new WslFileOperationAdapter(executionBoundary, wslFileSystem),
            new WindowsToWslCopyTransfer(executionBoundary, wslFileSystem))
    {
    }

    internal ProviderFileOperationPort(
        IFileOperationPort windowsLocal,
        IFileOperationPort wsl,
        IFileOperationPort windowsLocalToWsl)
    {
        ArgumentNullException.ThrowIfNull(windowsLocal);
        ArgumentNullException.ThrowIfNull(wsl);
        ArgumentNullException.ThrowIfNull(windowsLocalToWsl);
        _windowsLocal = windowsLocal;
        _wsl = wsl;
        _windowsLocalToWsl = windowsLocalToWsl;
    }

    /// <inheritdoc />
    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path switch
        {
            WindowsLocalPath => _windowsLocal.InspectAsync(path, cancellationToken),
            WslPath => _wsl.InspectAsync(path, cancellationToken),
            _ => Task.FromResult(FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)),
        };
    }

    /// <inheritdoc />
    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        return Route(TransferRoute.Derive(sources, destination)) is IFileOperationPort port
            ? port.PreflightTransferAsync(sources, destination, cancellationToken)
            : Task.FromResult(TransferPreflightOutcome.Rejected(FileOperationFailureKind.ProviderUnavailable));
    }

    /// <inheritdoc />
    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return Route(TransferRoute.Derive(source.Path, destination)) is IFileOperationPort port
            ? port.GetAtomicMoveCapabilityAsync(source, destination, cancellationToken)
            : Task.FromResult(AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return Route(TransferRoute.Derive(source.Path, destination)) is IFileOperationPort port
            ? port.MoveAsync(source, destination, cancellationToken)
            : FailedStep();
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return Route(TransferRoute.Derive(source.Path, destination)) is IFileOperationPort port
            ? port.CopyAsync(source, destination, cancellationToken)
            : FailedStep();
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return Route(TransferRoute.Derive(source.Path, destination)) is IFileOperationPort port
            ? port.VerifyCopyAsync(source, destination, cancellationToken)
            : FailedStep();
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mode);
        return SourcePort(source.Path) is IFileOperationPort port
            ? port.DeleteAsync(source, mode, cancellationToken)
            : FailedStep();
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(target);
        return SourcePort(location.Path) is IFileOperationPort port
            ? port.CreateDirectoryAsync(location, target, cancellationToken)
            : FailedStep();
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return SourcePort(source.Path) is IFileOperationPort port
            ? port.RenameAsync(source, target, cancellationToken)
            : FailedStep();
    }

    private IFileOperationPort? Route(TransferRoute route)
    {
        return route switch
        {
            TransferRoute.SameWindowsLocal => _windowsLocal,
            TransferRoute.SameWslDistribution => _wsl,
            TransferRoute.WindowsLocalToWsl => _windowsLocalToWsl,
            _ => null,
        };
    }

    private IFileOperationPort? SourcePort(FileSystemPath source)
    {
        return source switch
        {
            WindowsLocalPath => _windowsLocal,
            WslPath => _wsl,
            _ => null,
        };
    }

    private static Task<ProviderStepOutcome> FailedStep()
    {
        return Task.FromResult(ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }
}
