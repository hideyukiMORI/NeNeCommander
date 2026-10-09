using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Owns one directory read a pane session started (ADR-0058): the token source linked to the
/// caller's token, the provider task, and the abandonment the waiting caller also observes. The
/// provider task is always observed exactly once: by the waiting caller while the read is not
/// abandoned, and by this read's completion callback after it is, so an abandoned read that faults
/// reaches the session as an orphaned fault instead of an unobserved task exception.
/// </summary>
internal sealed class PaneRead
{
    private readonly TaskCompletionSource<PaneSnapshot> _abandonment;
    private readonly CancellationTokenSource _cancellation;
    private readonly Action<ExceptionDispatchInfo> _orphanedFault;

    private PaneRead(CancellationTokenSource cancellation, Task<DirectoryReadOutcome> provider, Action<ExceptionDispatchInfo> orphanedFault)
    {
        _cancellation = cancellation;
        _orphanedFault = orphanedFault;
        _abandonment = new TaskCompletionSource<PaneSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        Provider = provider;
    }

    /// <summary>Gets the provider task; it runs to completion even after the read is abandoned.</summary>
    internal Task<DirectoryReadOutcome> Provider { get; }

    /// <summary>Gets the task that completes with the abandoned snapshot when the read is abandoned.</summary>
    internal Task<PaneSnapshot> Abandonment => _abandonment.Task;

    /// <summary>Gets whether the user abandoned this read.</summary>
    internal bool IsAbandoned => _abandonment.Task.IsCompleted;

    /// <summary>
    /// Starts the provider read with a token source linked to the caller's token. The source is
    /// disposed when the provider task completes, or at once when the provider throws synchronously.
    /// </summary>
    internal static PaneRead Start(
        IDirectoryReadPort port,
        DirectoryReadRequest request,
        Action<ExceptionDispatchInfo> orphanedFault,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<DirectoryReadOutcome> provider;
        try
        {
            provider = port.ReadAsync(request, cancellation.Token);
        }
        catch
        {
            cancellation.Dispose();
            throw;
        }
        PaneRead read = new(cancellation, provider, orphanedFault);
        provider.GetAwaiter().OnCompleted(read.Release);
        return read;
    }

    /// <summary>
    /// Completes the waiting caller with the abandoned snapshot and cancels the read's own token
    /// while the provider is still running. A provider that already completed has released its
    /// token source, so it is not touched.
    /// </summary>
    internal void Abandon(PaneSnapshot abandoned)
    {
        _abandonment.SetResult(abandoned);
        if (!Provider.IsCompleted)
        {
            _cancellation.Cancel();
        }
    }

    private void Release()
    {
        _cancellation.Dispose();
        if (IsAbandoned && Provider.Exception is AggregateException fault)
        {
            _orphanedFault(ExceptionDispatchInfo.Capture(fault.InnerExceptions[0]));
        }
    }
}
