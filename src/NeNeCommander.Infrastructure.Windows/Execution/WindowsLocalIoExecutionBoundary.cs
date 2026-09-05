using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Infrastructure.Windows.Execution;

/// <summary>
/// Owns the single scheduling boundary that moves synchronous Windows filesystem work away
/// from its caller while leaving completion and fault observation with the awaiting owner.
/// </summary>
public sealed class WindowsLocalIoExecutionBoundary
{
    private readonly IWindowsLocalIoScheduler _scheduler;

    /// <summary>Initializes a boundary that schedules work on the process default scheduler.</summary>
    public WindowsLocalIoExecutionBoundary()
        : this(new DefaultWindowsLocalIoScheduler())
    {
    }

    internal WindowsLocalIoExecutionBoundary(IWindowsLocalIoScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        _scheduler = scheduler;
    }

    internal Task<TResult> ExecuteAsync<TResult>(Func<TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return _scheduler.ScheduleAsync(operation);
    }

    private sealed class DefaultWindowsLocalIoScheduler : IWindowsLocalIoScheduler
    {
        public Task<TResult> ScheduleAsync<TResult>(Func<TResult> operation)
        {
            return Task.Factory.StartNew(
                operation,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }
    }
}
