using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Exposes two independently released capability answers and their ordered call contexts.</summary>
internal sealed class DeferredCapabilityPort : IFileOperationPort
{
    private readonly Lock _sync = new();
    private readonly ScriptedFileOperationPort _inner = ScriptedFileOperationPort.Create(null, null);
    private readonly TaskCompletionSource<AtomicMoveCapabilityOutcome>[] _answers =
    [
        new(TaskCreationOptions.RunContinuationsAsynchronously),
        new(TaskCreationOptions.RunContinuationsAsynchronously),
    ];
    private readonly TaskCompletionSource _secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<(string Call, SynchronizationContext? Context)> _calls = [];
    private int _queried;

    internal DeferredCapabilityPort()
    {
        FileSystemPath first = GatewayTestContext.ParsePath("C:\\first");
        FileSystemPath second = GatewayTestContext.ParsePath("C:\\second");
        Request = GatewayTestContext.CreateMove([first, second]);
        _inner.EnqueueInspection(GatewayTestContext.Inspection(first, DeletionCapability.NotRequested));
        _inner.EnqueueInspection(GatewayTestContext.Inspection(second, DeletionCapability.NotRequested));
        _inner.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        _inner.EnqueueAtomicMove(ProviderStepOutcome.Succeeded());
        _inner.EnqueueAtomicMove(ProviderStepOutcome.Succeeded());
    }

    internal MoveRequest Request { get; }
    internal Task SecondStarted => _secondStarted.Task;
    internal IReadOnlyList<(string Call, SynchronizationContext? Context)> Calls
    {
        get
        {
            lock (_sync)
            {
                return _calls.ToList().AsReadOnly();
            }
        }
    }

    internal void Complete(int index, AtomicMoveCapabilityOutcome outcome)
    {
        _answers[index].SetResult(outcome);
    }

    internal void Fault(int index, Exception exception)
    {
        _answers[index].SetException(exception);
    }

    internal void RejectNextInspection()
    {
        _inner.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _calls.Add(("Capability:" + source.Path.CanonicalText, SynchronizationContext.Current));
            Task<AtomicMoveCapabilityOutcome> answer = _answers[_queried++].Task;
            if (_queried == 2)
            {
                _secondStarted.SetResult();
            }
            return answer;
        }
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _calls.Add(("Move:" + source.Path.CanonicalText, SynchronizationContext.Current));
        }
        return _inner.MoveAsync(source, destination, cancellationToken);
    }

    public Task<FileInspectionOutcome> InspectAsync(
        FileSystemPath path, FileInspectionPurpose purpose, CancellationToken cancellationToken)
    {
        return _inner.InspectAsync(path, purpose, cancellationToken);
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources, FileSystemPath destination, CancellationToken cancellationToken)
    {
        return _inner.PreflightTransferAsync(sources, destination, cancellationToken);
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
    {
        return _inner.CopyAsync(source, destination, cancellationToken);
    }

    public Task<CopyVerificationOutcome> VerifyCopyAsync(
        FileEntrySnapshot source, FileSystemPath destination, CancellationToken cancellationToken)
    {
        return _inner.VerifyCopyAsync(source, destination, cancellationToken);
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source, DeletionExecutionMode mode, CancellationToken cancellationToken)
    {
        return _inner.DeleteAsync(source, mode, cancellationToken);
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location, FileSystemPath target, CancellationToken cancellationToken)
    {
        return _inner.CreateDirectoryAsync(location, target, cancellationToken);
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source, FileSystemPath target, CancellationToken cancellationToken)
    {
        return _inner.RenameAsync(source, target, cancellationToken);
    }
}
