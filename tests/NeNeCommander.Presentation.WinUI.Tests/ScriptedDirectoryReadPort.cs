using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Tests;

internal sealed class ScriptedDirectoryReadPort : IDirectoryReadPort
{
    private readonly Queue<TaskCompletionSource<DirectoryReadOutcome>> _reads;

    private ScriptedDirectoryReadPort()
    {
        _reads = [];
    }

    internal List<CancellationToken> Tokens { get; } = [];

    internal static ScriptedDirectoryReadPort Create()
    {
        return new ScriptedDirectoryReadPort();
    }

    internal void Enqueue(DirectoryReadOutcome outcome)
    {
        TaskCompletionSource<DirectoryReadOutcome> completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        completed.SetResult(outcome);
        _reads.Enqueue(completed);
    }

    internal TaskCompletionSource<DirectoryReadOutcome> EnqueuePending()
    {
        TaskCompletionSource<DirectoryReadOutcome> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _reads.Enqueue(pending);
        return pending;
    }

    /// <summary>Queues a provider whose completion callback finishes inline for late-result assertions.</summary>
    internal TaskCompletionSource<DirectoryReadOutcome> EnqueuePendingInline()
    {
        TaskCompletionSource<DirectoryReadOutcome> pending = new();
        _reads.Enqueue(pending);
        return pending;
    }
    public Task<DirectoryReadOutcome> ReadAsync(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        Tokens.Add(cancellationToken);
        return _reads.Dequeue().Task;
    }
}
