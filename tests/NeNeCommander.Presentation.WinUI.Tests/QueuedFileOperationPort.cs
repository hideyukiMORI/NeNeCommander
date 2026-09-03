using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

internal sealed class QueuedFileOperationPort : IFileOperationPort
{
    private readonly Queue<FileInspectionOutcome> _inspections;
    private readonly Queue<ProviderStepOutcome> _steps;

    private QueuedFileOperationPort()
    {
        _inspections = [];
        _steps = [];
    }

    internal static QueuedFileOperationPort Create()
    {
        return new QueuedFileOperationPort();
    }

    internal void EnqueueInspection(FileInspectionOutcome outcome)
    {
        _inspections.Enqueue(outcome);
    }

    internal void EnqueueStep(ProviderStepOutcome outcome)
    {
        _steps.Enqueue(outcome);
    }

    public Task<FileInspectionOutcome> InspectAsync(FileSystemPath path, CancellationToken cancellationToken)
    {
        return Task.FromResult(_inspections.Dequeue());
    }

    public Task<ProviderStepOutcome> PreflightMoveAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }

    public Task<ProviderStepOutcome> CopyAsync(
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

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_steps.Dequeue());
    }
}
