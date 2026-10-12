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
    private readonly IWindowsLocalIoScheduler _shellScheduler;

    /// <summary>Initializes a boundary that schedules work on the process default scheduler.</summary>
    public WindowsLocalIoExecutionBoundary()
        : this(new DefaultWindowsLocalIoScheduler(), new WindowsShellIoScheduler())
    {
    }

    internal WindowsLocalIoExecutionBoundary(IWindowsLocalIoScheduler scheduler)
        : this(scheduler, new WindowsShellIoScheduler())
    {
    }

    internal WindowsLocalIoExecutionBoundary(
        IWindowsLocalIoScheduler scheduler,
        IWindowsLocalIoScheduler shellScheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(shellScheduler);
        _scheduler = scheduler;
        _shellScheduler = shellScheduler;
    }

    internal Task<TResult> ExecuteAsync<TResult>(Func<TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return _scheduler.ScheduleAsync(operation);
    }

    internal Task<TResult> ExecuteShellAsync<TResult>(Func<TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return _shellScheduler.ScheduleAsync(operation);
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
