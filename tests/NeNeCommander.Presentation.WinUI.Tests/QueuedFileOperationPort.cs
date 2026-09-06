using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

internal sealed class QueuedFileOperationPort : IFileOperationPort
{
    private readonly Queue<TaskCompletionSource<FileInspectionOutcome>> _inspections;
    private readonly Queue<ProviderStepOutcome> _steps;
    private readonly Queue<TransferPreflightOutcome> _preflights;

    private QueuedFileOperationPort()
    {
        _inspections = [];
        _steps = [];
        _preflights = [];
    }

    internal static QueuedFileOperationPort Create()
    {
        return new QueuedFileOperationPort();
    }

    internal void EnqueueInspection(FileInspectionOutcome outcome)
    {
        TaskCompletionSource<FileInspectionOutcome> completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        completed.SetResult(outcome);
        _inspections.Enqueue(completed);
    }

    internal TaskCompletionSource<FileInspectionOutcome> EnqueuePendingInspection()
    {
        TaskCompletionSource<FileInspectionOutcome> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _inspections.Enqueue(pending);
        return pending;
    }

    internal void EnqueueStep(ProviderStepOutcome outcome)
    {
        _steps.Enqueue(outcome);
    }

    internal void EnqueuePreflight(TransferPreflightOutcome outcome)
    {
        _preflights.Enqueue(outcome);
    }

    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        return _inspections.Dequeue().Task;
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        if (_preflights.Count > 0)
        {
            return Task.FromResult(_preflights.Dequeue());
        }
        ProviderStepOutcome step = _steps.Dequeue();
        if (step.Failure is FileOperationFailureKind failure)
        {
            return Task.FromResult(TransferPreflightOutcome.Rejected(failure));
        }
        List<TransferPlanEntry> plan = [];
        foreach (FileEntrySnapshot source in sources)
        {
            string name = source.Path.CanonicalText[(source.Path.CanonicalText.LastIndexOf('\\') + 1)..];
            plan.Add(TransferPlanEntry.Transfer(source, ((PathParseSuccess)destination.Child(name)).Path));
        }
        return Task.FromResult(TransferPreflightOutcome.Succeeded(plan));
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(AtomicMoveCapabilityOutcome.Unsupported);
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }

    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }
}
