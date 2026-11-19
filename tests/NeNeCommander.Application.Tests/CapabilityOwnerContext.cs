using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Application.Tests;

/// <summary>Holds operation-owner continuations until the test explicitly admits one.</summary>
internal sealed class CapabilityOwnerContext : SynchronizationContext
{
    private readonly Lock _sync = new();
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = [];
    private TaskCompletionSource _posted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task Posted
    {
        get
        {
            lock (_sync)
            {
                return _posted.Task;
            }
        }
    }

    internal Task<TResult> StartAsync<TResult>(Func<Task<TResult>> start)
    {
        SynchronizationContext? previous = Current;
        SetSynchronizationContext(this);
        try
        {
            return start();
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }

    public override void Post(SendOrPostCallback callback, object? state)
    {
        lock (_sync)
        {
            _pending.Enqueue((callback, state));
            _ = _posted.TrySetResult();
        }
    }

    internal void Advance()
    {
        (SendOrPostCallback Callback, object? State) work;
        lock (_sync)
        {
            work = _pending.Dequeue();
            if (_pending.Count == 0)
            {
                _posted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
        SynchronizationContext? previous = Current;
        SetSynchronizationContext(this);
        try
        {
            work.Callback(work.State);
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }
}
