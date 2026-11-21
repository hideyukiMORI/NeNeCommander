using System;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// Retains the bookmark view's latest deferred focus request. Explicit search or leaving a state
/// cancels it; incidental focus cancels restoration but retains a requested state transition.
/// The view supplies both scheduling and the native focus action and remains their owner.
/// </summary>
internal sealed class BookmarkFocusRestoration
{
    private readonly Action<Action> _defer;
    private PendingFocus? _pending;
    private PendingFocus? _explicitRequest;

    internal BookmarkFocusRestoration(Action<Action> defer)
    {
        ArgumentNullException.ThrowIfNull(defer);
        _defer = defer;
    }

    internal bool IsPending => _pending is not null;

    internal void Request(Action focus)
    {
        ArgumentNullException.ThrowIfNull(focus);
        PendingFocus request = new(focus);
        _explicitRequest = request;
        Enqueue(request);
    }

    private void Enqueue(PendingFocus request)
    {
        _pending = request;
        _defer(() =>
        {
            if (!ReferenceEquals(_pending, request))
            {
                return;
            }
            _pending = null;
            _explicitRequest = null;
            request.Focus();
        });
    }

    internal void Restore(Action focus)
    {
        ArgumentNullException.ThrowIfNull(focus);
        if (_pending is null)
        {
            _explicitRequest = null;
            Enqueue(new PendingFocus(focus));
        }
    }

    internal void CancelRestoration()
    {
        if (!ReferenceEquals(_pending, _explicitRequest))
        {
            Cancel();
        }
    }

    internal void Cancel()
    {
        _pending = null;
        _explicitRequest = null;
    }

    private sealed class PendingFocus
    {
        internal PendingFocus(Action focus)
        {
            Focus = focus;
        }

        internal Action Focus { get; }
    }
}
