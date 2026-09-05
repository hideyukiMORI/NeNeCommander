using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Presentation.WinUI.Lifecycle;

/// <summary>
/// Owns one replaceable asynchronous UI work item, observes defects as soon as work completes,
/// and closes in-flight work in cancel, await, dispose order.
/// </summary>
public sealed class AsyncWorkOwner
{
    private readonly Action<Exception> _defectObserver;
    private readonly Action _cancellationDisposed;
    private readonly Func<CancellationTokenSource> _cancellationFactory;
    private readonly Lock _sync;
    private Exception? _fault;
    private OwnedRun? _run;

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
                return _run is not null;
            }
        }
    }

    /// <summary>
    /// Starts work only when no prior work is running or faulted. A successfully completed prior
    /// run is disposed before its replacement is created.
    /// </summary>
    /// <param name="work">Work factory receiving the token owned by this instance.</param>
    /// <returns><see langword="true"/> when this call started the work.</returns>
    public bool TryStart(Func<CancellationToken, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        lock (_sync)
        {
            if (_fault is not null || _run is { Work.IsCompleted: false })
            {
                return false;
            }
            if (_run is not null)
            {
                CompleteRun(_run);
            }
            if (_fault is not null)
            {
                return false;
            }
            CancellationTokenSource cancellation = _cancellationFactory();
            ArgumentNullException.ThrowIfNull(cancellation);
            Task startedWork = StartWork(work, cancellation);
            OwnedRun run = new(cancellation, startedWork);
            _run = run;
            startedWork.GetAwaiter().OnCompleted(() => CompleteRun(run));
            return true;
        }
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

    /// <summary>Cancels running work, awaits its completion, then disposes its token owner.</summary>
    public async Task StopAsync()
    {
        OwnedRun? run;
        lock (_sync)
        {
            run = _run;
            if (run is null)
            {
                return;
            }
            if (!run.Work.IsCompleted)
            {
                run.Cancellation.Cancel();
            }
        }
        await run.Completion.Task;
    }

    private void CompleteRun(OwnedRun run)
    {
        Exception? defect;
        lock (_sync)
        {
            if (run.IsDisposed)
            {
                return;
            }
            defect = RecordFault(run);
            _run = null;
            run.IsDisposed = true;
            run.Cancellation.Dispose();
            _cancellationDisposed();
            run.Completion.SetResult();
        }
        if (defect is not null)
        {
            _defectObserver(defect);
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

        internal bool IsDisposed { get; set; }

        internal Task Work { get; }
    }
}
