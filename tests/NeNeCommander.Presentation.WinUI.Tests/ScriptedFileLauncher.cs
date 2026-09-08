using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Launching;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Returns test-owned file-launch outcomes without invoking an external application.</summary>
internal sealed class ScriptedFileLauncher : IFileLauncher
{
    private readonly Queue<Task<FileLaunchOutcome>> _outcomes = new();

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
        _ = target;
        _ = cancellationToken;
        return _outcomes.Dequeue();
    }
}
