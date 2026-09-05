using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Routes one canonical mutation port to Windows-side provider adapters.</summary>
public sealed class ProviderFileOperationPort : IFileOperationPort
{
    private readonly IFileOperationPort _windowsLocal;
    private readonly IFileOperationPort _wsl;

    /// <summary>Initializes the provider router over the shared Windows I/O execution boundary.</summary>
    public ProviderFileOperationPort(WindowsLocalIoExecutionBoundary executionBoundary)
        : this(
            new WindowsLocalFileOperationAdapter(executionBoundary),
            new WslFileOperationAdapter(executionBoundary))
    {
    }

    internal ProviderFileOperationPort(IFileOperationPort windowsLocal, IFileOperationPort wsl)
    {
        ArgumentNullException.ThrowIfNull(windowsLocal);
        ArgumentNullException.ThrowIfNull(wsl);
        _windowsLocal = windowsLocal;
        _wsl = wsl;
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
    public Task<ProviderStepOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destination);
        return Select(sources) switch
        {
            WindowsLocalPath => _windowsLocal.PreflightTransferAsync(sources, destination, cancellationToken),
            WslPath => _wsl.PreflightTransferAsync(sources, destination, cancellationToken),
            _ => FailedStep(),
        };
    }

    /// <inheritdoc />
    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return source.Path switch
        {
            WindowsLocalPath => _windowsLocal.GetAtomicMoveCapabilityAsync(source, destination, cancellationToken),
            WslPath => _wsl.GetAtomicMoveCapabilityAsync(source, destination, cancellationToken),
            _ => Task.FromResult(
                AtomicMoveCapabilityOutcome.Failed(FileOperationFailureKind.ProviderUnavailable)),
        };
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Select(source, destination, _windowsLocal.MoveAsync, _wsl.MoveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Select(source, destination, _windowsLocal.CopyAsync, _wsl.CopyAsync, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Select(
            source,
            destination,
            _windowsLocal.VerifyCopyAsync,
            _wsl.VerifyCopyAsync,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mode);
        return source.Path switch
        {
            WindowsLocalPath => _windowsLocal.DeleteAsync(source, mode, cancellationToken),
            WslPath => _wsl.DeleteAsync(source, mode, cancellationToken),
            _ => FailedStep(),
        };
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Select(
            location,
            target,
            _windowsLocal.CreateDirectoryAsync,
            _wsl.CreateDirectoryAsync,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Select(source, target, _windowsLocal.RenameAsync, _wsl.RenameAsync, cancellationToken);
    }

    private static FileSystemPath? Select(IReadOnlyList<FileEntrySnapshot> sources)
    {
        if (sources.Count == 0)
        {
            return null;
        }

        FileSystemPath first = sources[0].Path;
        return sources.All(source => HasSameProvider(first, source.Path)) ? first : null;
    }

    private static bool HasSameProvider(FileSystemPath first, FileSystemPath candidate)
    {
        return (first, candidate) switch
        {
            (WindowsLocalPath, WindowsLocalPath) => true,
            (WslPath firstWsl, WslPath candidateWsl) => firstWsl.DistributionName.Equals(
                candidateWsl.DistributionName,
                StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static Task<ProviderStepOutcome> Select(
        FileEntrySnapshot source,
        FileSystemPath target,
        Func<FileEntrySnapshot, FileSystemPath, CancellationToken, Task<ProviderStepOutcome>> windowsLocal,
        Func<FileEntrySnapshot, FileSystemPath, CancellationToken, Task<ProviderStepOutcome>> wsl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return source.Path switch
        {
            WindowsLocalPath => windowsLocal(source, target, cancellationToken),
            WslPath => wsl(source, target, cancellationToken),
            _ => FailedStep(),
        };
    }

    private static Task<ProviderStepOutcome> FailedStep()
    {
        return Task.FromResult(ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }
}
