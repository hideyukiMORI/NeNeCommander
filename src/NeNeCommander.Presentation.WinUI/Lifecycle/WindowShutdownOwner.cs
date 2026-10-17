using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Presentation.WinUI.Lifecycle;

/// <summary>
/// Owns one normal window-close request. It keeps the final close behind the supplied asynchronous
/// drain and publishes any drain or close defect through the existing asynchronous work owner.
/// </summary>
public sealed class WindowShutdownOwner
{
    private readonly Action _close;
    private readonly Func<Task> _drain;
    private readonly AsyncWorkOwner _work;
    private readonly Lock _sync = new();
    private bool _requested;

    /// <summary>Creates the sole owner of the drain and successful final-close callback.</summary>
    /// <param name="drain">Completes admitted work and cleanup before the window can close.</param>
    /// <param name="close">Requests final close after a successful drain.</param>
    /// <param name="defectObserver">Publishes an unexpected drain or close defect.</param>
    public WindowShutdownOwner(Func<Task> drain, Action close, Action<Exception> defectObserver)
    {
        ArgumentNullException.ThrowIfNull(drain);
        ArgumentNullException.ThrowIfNull(close);
        ArgumentNullException.ThrowIfNull(defectObserver);
        _drain = drain;
        _close = close;
        _work = new AsyncWorkOwner(defectObserver);
    }

    /// <summary>
    /// Accepts the first close request and starts its owned drain. Later requests are rejected,
    /// including while the drain is pending or after it faults or finishes.
    /// </summary>
    /// <returns><see langword="true"/> only for the first accepted request.</returns>
    public bool RequestClose()
    {
        lock (_sync)
        {
            if (_requested)
            {
                return false;
            }
            _requested = true;
            return _work.TryStart(_ => DrainAndCloseAsync());
        }
    }

    private async Task DrainAndCloseAsync()
    {
        await _drain();
        _close();
    }
}
