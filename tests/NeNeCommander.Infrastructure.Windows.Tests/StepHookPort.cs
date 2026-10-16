using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Records the transfer steps the gateway sends through the router and runs an optional hook
/// right after a copy or verification step returns, at the declared port boundary.
/// </summary>
internal sealed class StepHookPort : IFileOperationPort
{
    private readonly IFileOperationPort _inner;

    internal StepHookPort(IFileOperationPort inner)
    {
        _inner = inner;
    }

    internal Func<Task> AfterPreflight { get; init; } = () => Task.CompletedTask;

    internal Func<FileEntrySnapshot, Task> AfterCopy { get; init; } = _ => Task.CompletedTask;

    internal Func<FileEntrySnapshot, Task> AfterVerify { get; init; } = _ => Task.CompletedTask;

    internal Func<FileEntrySnapshot, DeletionExecutionMode, DeletionExecutionMode> TransformDeletion { get; init; } = (_, mode) => mode;

    internal List<string> Calls { get; } = [];

    internal List<DeletionExecutionMode> DeletionModes { get; } = [];

    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        return _inner.InspectAsync(path, cancellationToken);
    }

    public async Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        TransferPreflightOutcome outcome = await _inner.PreflightTransferAsync(sources, destination, cancellationToken);
        await AfterPreflight();
        return outcome;
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        Calls.Add("capability");
        return _inner.GetAtomicMoveCapabilityAsync(source, destination, cancellationToken);
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        Calls.Add("move");
        return _inner.MoveAsync(source, destination, cancellationToken);
    }

    public async Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        Calls.Add("copy");
        ProviderStepOutcome outcome = await _inner.CopyAsync(source, destination, cancellationToken);
        await AfterCopy(source);
        return outcome;
    }

    public async Task<CopyVerificationOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        Calls.Add("verify");
        CopyVerificationOutcome outcome = await _inner.VerifyCopyAsync(source, destination, cancellationToken);
        await AfterVerify(source);
        return outcome;
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        Calls.Add("delete");
        DeletionExecutionMode instruction = TransformDeletion(source, mode);
        DeletionModes.Add(instruction);
        return _inner.DeleteAsync(source, instruction, cancellationToken);
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return _inner.CreateDirectoryAsync(location, target, cancellationToken);
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return _inner.RenameAsync(source, target, cancellationToken);
    }
}
