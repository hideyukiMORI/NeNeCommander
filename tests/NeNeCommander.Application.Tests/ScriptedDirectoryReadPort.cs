using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Application.Tests;

internal sealed class ScriptedDirectoryReadPort : IDirectoryReadPort
{
    private readonly Queue<TaskCompletionSource<DirectoryReadOutcome>> _reads;
    private readonly List<DirectoryReadRequest> _requests;

    private ScriptedDirectoryReadPort()
    {
        _reads = [];
        _requests = [];
    }

    internal IReadOnlyList<DirectoryReadRequest> Requests => new ReadOnlyCollection<DirectoryReadRequest>(_requests);

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

    public Task<DirectoryReadOutcome> ReadAsync(DirectoryReadRequest request, CancellationToken cancellationToken)
    {
        _requests.Add(request);
        return _reads.Dequeue().Task;
    }
}
