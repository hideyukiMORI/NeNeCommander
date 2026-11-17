using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Queues work completions until a test explicitly advances the next callback.</summary>
internal sealed class ControlledSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = [];
    private readonly Lock _sync = new();
    private readonly TaskCompletionSource _posted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task Posted => _posted.Task;

    public override void Post(SendOrPostCallback callback, object? state)
    {
        lock (_sync)
        {
            _pending.Enqueue((callback, state));
            _ = _posted.TrySetResult();
        }
    }

    internal void ExecuteOne()
    {
        (SendOrPostCallback Callback, object? State) work;
        lock (_sync)
        {
            work = _pending.Dequeue();
        }
        work.Callback(work.State);
    }
}
