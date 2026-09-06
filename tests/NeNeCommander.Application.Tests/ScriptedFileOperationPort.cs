using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

internal sealed class ScriptedFileOperationPort : IFileOperationPort
{
    private readonly Action? _callback;
    private readonly ScriptedCallbackPoint? _callbackPoint;
    private readonly List<string> _calls;
    private readonly Queue<ProviderStepOutcome> _copies;
    private readonly Queue<ProviderStepOutcome> _deletions;
    private readonly Queue<ProviderStepOutcome> _directoryCreations;
    private readonly Queue<AtomicMoveCapabilityOutcome> _atomicMoveCapabilities;
    private readonly Queue<ProviderStepOutcome> _atomicMoves;
    private readonly Queue<FileInspectionOutcome> _inspections;
    private readonly Queue<ProviderStepOutcome> _preflights;
    private readonly Queue<TransferPreflightOutcome> _transferPreflights;
    private readonly Queue<ProviderStepOutcome> _renames;
    private readonly Queue<ProviderStepOutcome> _verifications;

    private ScriptedFileOperationPort(ScriptedCallbackPoint? callbackPoint, Action? callback)
    {
        _callbackPoint = callbackPoint;
        _callback = callback;
        _calls = [];
        _copies = [];
        _deletions = [];
        _directoryCreations = [];
        _atomicMoveCapabilities = [];
        _atomicMoves = [];
        _inspections = [];
        _preflights = [];
        _transferPreflights = [];
        _renames = [];
        _verifications = [];
    }

    internal IReadOnlyList<string> Calls => new ReadOnlyCollection<string>(_calls);

    internal static ScriptedFileOperationPort Create(
        ScriptedCallbackPoint? callbackPoint,
        Action? callback)
    {
        return new ScriptedFileOperationPort(callbackPoint, callback);
    }

    internal void EnqueueInspection(FileInspectionOutcome outcome)
    {
        _inspections.Enqueue(outcome);
    }

    internal void EnqueuePreflight(ProviderStepOutcome outcome)
    {
        _preflights.Enqueue(outcome);
    }

    internal void EnqueuePreflight(TransferPreflightOutcome outcome)
    {
        _transferPreflights.Enqueue(outcome);
    }

    internal void EnqueueCopy(ProviderStepOutcome outcome)
    {
        _copies.Enqueue(outcome);
    }

    internal void EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome outcome)
    {
        _atomicMoveCapabilities.Enqueue(outcome);
    }

    internal void EnqueueAtomicMove(ProviderStepOutcome outcome)
    {
        _atomicMoves.Enqueue(outcome);
    }

    internal void EnqueueVerification(ProviderStepOutcome outcome)
    {
        _verifications.Enqueue(outcome);
    }

    internal void EnqueueDeletion(ProviderStepOutcome outcome)
    {
        _deletions.Enqueue(outcome);
    }

    internal void EnqueueDirectoryCreation(ProviderStepOutcome outcome)
    {
        _directoryCreations.Enqueue(outcome);
    }

    internal void EnqueueRename(ProviderStepOutcome outcome)
    {
        _renames.Enqueue(outcome);
    }

    public Task<FileInspectionOutcome> InspectAsync(
        FileSystemPath path,
        CancellationToken cancellationToken)
    {
        _calls.Add("Inspect:" + path.CanonicalText);
        FileInspectionOutcome outcome = _inspections.Dequeue();
        InvokeCallback(ScriptedCallbackPoint.AfterInspection);
        return Task.FromResult(outcome);
    }

    public Task<TransferPreflightOutcome> PreflightTransferAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        _calls.Add("Preflight:" + destination.CanonicalText);
        TransferPreflightOutcome outcome;
        if (_transferPreflights.Count > 0)
        {
            outcome = _transferPreflights.Dequeue();
        }
        else
        {
            ProviderStepOutcome legacy = _preflights.Dequeue();
            outcome = legacy.Failure is FileOperationFailureKind failure
                ? TransferPreflightOutcome.Rejected(failure)
                : TransferPreflightOutcome.Succeeded(CreatePlan(sources, destination));
        }
        InvokeCallback(ScriptedCallbackPoint.AfterPreflight);
        return Task.FromResult(outcome);
    }

    private static List<TransferPlanEntry> CreatePlan(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination)
    {
        List<TransferPlanEntry> plan = [];
        foreach (FileEntrySnapshot source in sources)
        {
            string name = source.Path.CanonicalText[(source.Path.CanonicalText.LastIndexOf('\\') + 1)..];
            FileSystemPath target = ((PathParseSuccess)destination.Child(name)).Path;
            plan.Add(TransferPlanEntry.Transfer(source, target));
        }
        return plan;
    }

    public Task<ProviderStepOutcome> CopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        _calls.Add("Copy:" + source.Path.CanonicalText);
        ProviderStepOutcome outcome = _copies.Dequeue();
        InvokeCallback(ScriptedCallbackPoint.AfterCopy);
        return Task.FromResult(outcome);
    }

    public Task<AtomicMoveCapabilityOutcome> GetAtomicMoveCapabilityAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        if (_atomicMoveCapabilities.Count == 0)
        {
            return Task.FromResult(AtomicMoveCapabilityOutcome.Unsupported);
        }
        _calls.Add("AtomicCapability:" + source.Path.CanonicalText);
        AtomicMoveCapabilityOutcome outcome = _atomicMoveCapabilities.Dequeue();
        InvokeCallback(ScriptedCallbackPoint.AfterAtomicCapability);
        return Task.FromResult(outcome);
    }

    public Task<ProviderStepOutcome> MoveAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        _calls.Add("AtomicMove:" + source.Path.CanonicalText);
        ProviderStepOutcome outcome = _atomicMoves.Dequeue();
        InvokeCallback(ScriptedCallbackPoint.AfterAtomicMove);
        return Task.FromResult(outcome);
    }

    public Task<ProviderStepOutcome> VerifyCopyAsync(
        FileEntrySnapshot source,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        _calls.Add("Verify:" + source.Path.CanonicalText);
        ProviderStepOutcome outcome = _verifications.Dequeue();
        InvokeCallback(ScriptedCallbackPoint.AfterVerification);
        return Task.FromResult(outcome);
    }

    public Task<ProviderStepOutcome> DeleteAsync(
        FileEntrySnapshot source,
        DeletionExecutionMode mode,
        CancellationToken cancellationToken)
    {
        string operation = mode == DeletionExecutionMode.Recycle ? "Recycle:" : "Delete:";
        _calls.Add(operation + source.Path.CanonicalText);
        ProviderStepOutcome outcome = _deletions.Dequeue();
        InvokeCallback(ScriptedCallbackPoint.AfterDeletion);
        return Task.FromResult(outcome);
    }

    public Task<ProviderStepOutcome> CreateDirectoryAsync(
        FileEntrySnapshot location,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        _calls.Add("CreateDirectory:" + target.CanonicalText);
        return Task.FromResult(_directoryCreations.Dequeue());
    }

    public Task<ProviderStepOutcome> RenameAsync(
        FileEntrySnapshot source,
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        _calls.Add("Rename:" + source.Path.CanonicalText + ">" + target.CanonicalText);
        return Task.FromResult(_renames.Dequeue());
    }

    private void InvokeCallback(ScriptedCallbackPoint point)
    {
        if (_callbackPoint == point)
        {
            _callback?.Invoke();
        }
    }
}
