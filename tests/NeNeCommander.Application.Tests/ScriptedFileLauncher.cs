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

    internal List<WindowsLocalPath> Targets { get; } = [];

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
        WindowsLocalPath target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        Targets.Add(target);
        CancellationTokens.Add(cancellationToken);
        return _outcomes.Count > 0
            ? _outcomes.Dequeue()
            : Task.FromResult(FileLaunchOutcome.Accepted());
    }
}
