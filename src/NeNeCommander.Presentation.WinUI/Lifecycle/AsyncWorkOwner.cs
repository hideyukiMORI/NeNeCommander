using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Lifecycle;

/// <summary>
/// Owns one replaceable asynchronous UI work item, observes defects as soon as work completes,
/// and closes in-flight work in cancel, await, dispose order. While that work runs it admits one
/// interrupt and only for <see cref="UserIntent.Escape"/>, so the user can cancel a running
/// operation (ADR-0018) or abandon a loading pane read (ADR-0058); every other overlap is rejected.
/// Stop permanently closes admission before cancelling and awaiting owned work.
/// </summary>
public sealed class AsyncWorkOwner
{
    private readonly Action<Exception> _defectObserver;
    private readonly Action _cancellationDisposed;
    private readonly Func<CancellationTokenSource> _cancellationFactory;
    private readonly Lock _sync;
    private Exception? _fault;
    private OwnedRun? _interrupt;
    private OwnedRun? _run;
    private bool _stopped;

    /// <summary>Initializes an owner that reports every observed defect through one host callback.</summary>
    /// <param name="defectObserver">Host callback that publishes an unexpected task defect.</param>
    public AsyncWorkOwner(Action<Exception> defectObserver)
        : this(defectObserver, static () => new CancellationTokenSource(), static () => { })
    {
    }

    internal AsyncWorkOwner(
        Action<Exception> defectObserver,
        Func<CancellationTokenSource> cancellationFactory,
        Action cancellationDisposed)
    {
        ArgumentNullException.ThrowIfNull(defectObserver);
        ArgumentNullException.ThrowIfNull(cancellationFactory);
        ArgumentNullException.ThrowIfNull(cancellationDisposed);
        _defectObserver = defectObserver;
        _cancellationFactory = cancellationFactory;
        _cancellationDisposed = cancellationDisposed;
        _sync = new Lock();
    }

    /// <summary>Gets the unexpected defect observed from owned work, if one occurred.</summary>
    public Exception? Fault
    {
        get
        {
            lock (_sync)
            {
                return _fault;
            }
        }
    }

    internal bool HasOwnedWork
    {
        get
        {
            lock (_sync)
            {
                return _run is not null || _interrupt is not null;
            }
        }
    }

    /// <summary>
    /// Starts work only before Stop and when no prior work is running or faulted. A successfully
    /// completed prior run is disposed before its replacement is created.
    /// </summary>
    /// <param name="work">Work factory receiving the token owned by this instance.</param>
    /// <returns><see langword="true"/> when this call started the work.</returns>
    public bool TryStart(Func<CancellationToken, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        lock (_sync)
        {
            if (_stopped || _fault is not null || _run is { Work.IsCompleted: false } || _interrupt is { Work.IsCompleted: false })
            {
                return false;
            }
            if (_run is not null)
            {
                CompleteRun(_run);
            }
            if (_interrupt is not null)
            {
                CompleteRun(_interrupt);
            }
            if (_fault is not null || _run is not null || _interrupt is not null)
            {
                return false;
            }
            _run = CreateRun(work);
            Observe(_run);
            return true;
        }
    }

    /// <summary>
    /// Starts the work of one intent. With no work running it is an ordinary start. While work runs,
    /// only <see cref="UserIntent.Escape"/> starts, as the single interrupt that runs beside it; a
    /// second interrupt while the first runs, any other intent, and any work after Stop or an
    /// observed defect are rejected.
    /// </summary>
    /// <param name="intent">Intent the work forwards; it decides whether the work may interrupt.</param>
    /// <param name="work">Work factory receiving a token owned by this instance.</param>
    /// <returns><see langword="true"/> when this call started the work.</returns>
    public bool TryStartIntent(UserIntent intent, Func<CancellationToken, Task> work)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(work);
        lock (_sync)
        {
            if (_stopped)
            {
                return false;
            }
            if (_run is not { Work.IsCompleted: false })
            {
                return TryStart(work);
            }
            if (intent != UserIntent.Escape || _interrupt is { Work.IsCompleted: false })
            {
                return false;
            }
            if (_interrupt is not null)
            {
                CompleteRun(_interrupt);
            }
            if (_fault is not null || _interrupt is not null)
            {
                return false;
            }
            _interrupt = CreateRun(work);
            Observe(_interrupt);
            return true;
        }
    }

    private OwnedRun CreateRun(Func<CancellationToken, Task> work)
    {
        CancellationTokenSource cancellation = _cancellationFactory();
        ArgumentNullException.ThrowIfNull(cancellation);
        return new OwnedRun(cancellation, StartWork(work, cancellation));
    }

    private void Observe(OwnedRun run)
    {
        run.Work.GetAwaiter().OnCompleted(() => CompleteRun(run));
    }

    private Task StartWork(Func<CancellationToken, Task> work, CancellationTokenSource cancellation)
    {
        try
        {
            Task startedWork = work(cancellation.Token);
            ArgumentNullException.ThrowIfNull(startedWork);
            return startedWork;
        }
        catch
        {
            cancellation.Dispose();
            _cancellationDisposed();
            throw;
        }
    }

    /// <summary>Closes admission, cancels running work and any interrupt, then awaits their cleanup and token disposal.</summary>
    public async Task StopAsync()
    {
        OwnedRun? run = null;
        OwnedRun? interrupt = null;
        try
        {
            lock (_sync)
            {
                _stopped = true;
                run = _run;
                interrupt = _interrupt;
                try
                {
                    CancelRunning(run);
                }
                finally
                {
                    CancelRunning(interrupt);
                }
            }
        }
        finally
        {
            try
            {
                await AwaitCompletionAsync(run);
            }
            finally
            {
                await AwaitCompletionAsync(interrupt);
            }
        }
    }

    private static void CancelRunning(OwnedRun? run)
    {
        if (run is { Work.IsCompleted: false })
        {
            run.Cancellation.Cancel();
        }
    }

    private static Task AwaitCompletionAsync(OwnedRun? run)
    {
        return run is null ? Task.CompletedTask : run.Completion.Task;
    }

    private void CompleteRun(OwnedRun run)
    {
        Exception? defect = null;
        bool claimed = false;
        try
        {
            lock (_sync)
            {
                if (run.IsCompleting)
                {
                    return;
                }
                run.IsCompleting = true;
                claimed = true;
                defect = RecordFault(run);
                run.Cancellation.Dispose();
                _cancellationDisposed();
            }
            if (defect is not null)
            {
                _defectObserver(defect);
            }
        }
        finally
        {
            if (claimed)
            {
                lock (_sync)
                {
                    if (ReferenceEquals(run, _run))
                    {
                        _run = null;
                    }
                    if (ReferenceEquals(run, _interrupt))
                    {
                        _interrupt = null;
                    }
                    run.Completion.SetResult();
                }
            }
        }
    }

    private Exception? RecordFault(OwnedRun run)
    {
        if (!run.Work.IsFaulted || run.Work.Exception is not AggregateException aggregate)
        {
            return null;
        }
        Exception defect = aggregate.InnerExceptions.Count == 1 ? aggregate.InnerException! : aggregate;
        _fault = defect;
        return defect;
    }

    private sealed class OwnedRun
    {
        internal OwnedRun(CancellationTokenSource cancellation, Task work)
        {
            Cancellation = cancellation;
            Completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Work = work;
        }

        internal CancellationTokenSource Cancellation { get; }

        internal TaskCompletionSource Completion { get; }

        internal bool IsCompleting { get; set; }

        internal Task Work { get; }
    }
}
