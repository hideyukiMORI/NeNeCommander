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
    private readonly Queue<FileInspectionOutcome> _inspections;
    private readonly Queue<ProviderStepOutcome> _preflights;
    private readonly Queue<ProviderStepOutcome> _verifications;

    private ScriptedFileOperationPort(ScriptedCallbackPoint? callbackPoint, Action? callback)
    {
        _callbackPoint = callbackPoint;
        _callback = callback;
        _calls = [];
        _copies = [];
        _deletions = [];
        _inspections = [];
        _preflights = [];
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

    internal void EnqueueCopy(ProviderStepOutcome outcome)
    {
        _copies.Enqueue(outcome);
    }

    internal void EnqueueVerification(ProviderStepOutcome outcome)
    {
        _verifications.Enqueue(outcome);
    }

    internal void EnqueueDeletion(ProviderStepOutcome outcome)
    {
        _deletions.Enqueue(outcome);
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

    public Task<ProviderStepOutcome> PreflightMoveAsync(
        IReadOnlyList<FileEntrySnapshot> sources,
        FileSystemPath destination,
        CancellationToken cancellationToken)
    {
        _calls.Add("Preflight:" + destination.CanonicalText);
        ProviderStepOutcome outcome = _preflights.Dequeue();
        InvokeCallback(ScriptedCallbackPoint.AfterPreflight);
        return Task.FromResult(outcome);
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

    private void InvokeCallback(ScriptedCallbackPoint point)
    {
        if (_callbackPoint == point)
        {
            _callback?.Invoke();
        }
    }
}
