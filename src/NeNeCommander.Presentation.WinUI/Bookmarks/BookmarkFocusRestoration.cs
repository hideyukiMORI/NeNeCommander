using System;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>
/// Retains the bookmark view's latest deferred focus request. Search, an observed focus change or
/// leaving browsing cancels it; replacing the request makes already-queued callbacks harmless.
/// The view supplies both scheduling and the native focus action and remains their owner.
/// </summary>
internal sealed class BookmarkFocusRestoration
{
    private readonly Action<Action> _defer;
    private PendingFocus? _pending;

    internal BookmarkFocusRestoration(Action<Action> defer)
    {
        ArgumentNullException.ThrowIfNull(defer);
        _defer = defer;
    }

    internal void Request(Action focus)
    {
        ArgumentNullException.ThrowIfNull(focus);
        PendingFocus request = new(focus);
        _pending = request;
        _defer(() =>
        {
            if (!ReferenceEquals(_pending, request))
            {
                return;
            }
            _pending = null;
            request.Focus();
        });
    }

    internal void Restore(Action focus)
    {
        if (_pending is null)
        {
            Request(focus);
        }
    }

    internal void Cancel()
    {
        _pending = null;
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
