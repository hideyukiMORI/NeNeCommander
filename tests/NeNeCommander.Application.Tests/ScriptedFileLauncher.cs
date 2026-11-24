using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Records file handoffs and returns test-owned outcomes without invoking the Shell.</summary>
internal sealed class ScriptedFileLauncher : IFileLauncher
{
    private readonly Queue<Task<FileLaunchOutcome>> _outcomes = new();

    internal List<FileSystemPath> Targets { get; } = [];

    internal List<FileLaunchPurpose> Purposes { get; } = [];

    internal List<CancellationToken> CancellationTokens { get; } = [];

    internal void Enqueue(FileLaunchOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        _outcomes.Enqueue(Task.FromResult(outcome));
    }

    internal TaskCompletionSource<FileLaunchOutcome> EnqueuePending()
    {
        TaskCompletionSource<FileLaunchOutcome> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _outcomes.Enqueue(completion.Task);
        return completion;
    }

    public Task<FileLaunchOutcome> LaunchAsync(
        FileSystemPath target,
        CancellationToken cancellationToken)
    {
        return HandoffAsync(target, FileLaunchPurpose.Activation, cancellationToken);
    }

    public Task<FileLaunchOutcome> RevealAsync(FileSystemPath target, CancellationToken cancellationToken)
    {
        return HandoffAsync(target, FileLaunchPurpose.Reveal, cancellationToken);
    }

    private Task<FileLaunchOutcome> HandoffAsync(
        FileSystemPath target, FileLaunchPurpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        Purposes.Add(purpose);
        Targets.Add(target);
        CancellationTokens.Add(cancellationToken);
        return _outcomes.Count > 0
            ? _outcomes.Dequeue()
            : Task.FromResult(FileLaunchOutcome.Accepted());
    }
}
