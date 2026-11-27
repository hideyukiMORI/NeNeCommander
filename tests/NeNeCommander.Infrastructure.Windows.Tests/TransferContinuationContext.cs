using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Records caller-context dependency while permitting a failed probe to drain its owned continuation.</summary>
internal sealed class TransferContinuationContext : SynchronizationContext
{
    private readonly Lock _sync = new();
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = [];
    private TaskCompletionSource _posted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _posts;

    internal int Posts
    {
        get
        {
            lock (_sync)
            {
                return _posts;
            }
        }
    }

    internal Task StartAsync(Func<Task> start)
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
            _posts++;
            _pending.Enqueue((callback, state));
            _ = _posted.TrySetResult();
        }
    }

    internal async Task DrainAsync(Task operation)
    {
        while (!operation.IsCompleted)
        {
            Task posted;
            lock (_sync)
            {
                posted = _posted.Task;
            }
            _ = await Task.WhenAny(operation, posted).ConfigureAwait(false);
            Advance();
        }
        await operation.ConfigureAwait(false);
    }

    private void Advance()
    {
        (SendOrPostCallback Callback, object? State) work;
        lock (_sync)
        {
            if (_pending.Count == 0)
            {
                return;
            }
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
