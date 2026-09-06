using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Coordinates the existing dual-pane session with the session-owned settings modal. Each inner
/// session remains the sole owner of its state; this coordinator only chooses which one receives
/// an intent and freezes pane work while settings are open.
/// </summary>
public sealed class CommanderSession
{
    private readonly DualPaneSession _panes;
    private readonly SettingsSession _settings;
    private int _bookmarkNavigationInProgress;

    /// <summary>Initializes the application session over its two declared state owners.</summary>
    /// <param name="panes">Sole dual-pane coordinator.</param>
    /// <param name="settings">Sole settings interaction owner.</param>
    public CommanderSession(DualPaneSession panes, SettingsSession settings)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(settings);
        _panes = panes;
        _settings = settings;
    }

    /// <summary>Gets the current complete application-session snapshot.</summary>
    public CommanderSnapshot Current => new(_panes.Current, _settings.Current);

    /// <summary>Reads one pane location unless the settings editor owns modal input.</summary>
    public async Task<CommanderSnapshot> NavigateAsync(
        PaneSide side,
        FileSystemPath location,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(location);
        if (Volatile.Read(ref _bookmarkNavigationInProgress) != 0 ||
            _settings.Current.Editor != SettingsEditorState.Closed)
        {
            return Current;
        }
        _ = await _panes.NavigateAsync(side, location, cancellationToken).ConfigureAwait(false);
        return Current;
    }

    /// <summary>Routes one typed intent to settings or panes under the current modal owner.</summary>
    public async Task<CommanderSnapshot> HandleAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(observer);
        SettingsEditorState editor = _settings.Current.Editor;
        if (Volatile.Read(ref _bookmarkNavigationInProgress) != 0)
        {
            return Current;
        }
        if (editor == SettingsEditorState.Open)
        {
            HandleSettingsIntent(intent, observer);
            return Current;
        }
        if (editor == SettingsEditorState.Bookmarks)
        {
            return await HandleBookmarkIntentAsync(intent, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        if (intent == UserIntent.OpenSettings)
        {
            if (!PaneInteractionIsFrozen())
            {
                _ = _settings.Open();
            }
            return Current;
        }
        if (intent == UserIntent.OpenBookmarks)
        {
            if (!BookmarkInteractionIsFrozen())
            {
                _ = _settings.OpenBookmarks();
            }
            return Current;
        }
        if (intent is BookmarkShortcutSelection shortcut)
        {
            return BookmarkInteractionIsFrozen()
                ? Current
                : await NavigateBookmarkAsync(
                    _settings.Current.Settings.Bookmarks.Find(shortcut.Slot),
                    observer,
                    cancellationToken).ConfigureAwait(false);
        }
        _ = await _panes.HandleAsync(intent, observer, cancellationToken).ConfigureAwait(false);
        return Current;
    }

    /// <summary>Awaits every settings write queued before application shutdown.</summary>
    public Task StopAsync()
    {
        return _settings.StopAsync();
    }

    private void HandleSettingsIntent(
        UserIntent intent,
        ICommanderProgressObserver observer)
    {
        if (intent == UserIntent.Escape)
        {
            _ = _settings.Close();
            return;
        }
        _ = QueueSettingsSelection(intent, observer, CancellationToken.None);
    }

    private Task QueueSettingsSelection(
        UserIntent intent,
        ISettingsProgressObserver observer,
        CancellationToken cancellationToken)
    {
        return intent switch
        {
            ColorSchemeSelection selection =>
                _settings.SelectColorSchemeAsync(selection.Scheme, observer, cancellationToken),
            LaunchHiddenItemVisibilitySelection selection =>
                _settings.SelectLaunchHiddenItemVisibilityAsync(selection.Visibility, observer, cancellationToken),
            _ => Task.CompletedTask,
        };
    }

    private async Task<CommanderSnapshot> HandleBookmarkIntentAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (intent == UserIntent.Escape)
        {
            _ = _settings.Close();
            return Current;
        }
        if (intent is not BookmarkNavigationSelection navigation)
        {
            return Current;
        }
        BookmarkEntry? current =
            _settings.Current.Settings.Bookmarks.Find(navigation.Selection.Key);
        return current != navigation.Selection.Entry
            ? Current
            : await NavigateBookmarkAsync(current, observer, cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task<CommanderSnapshot> NavigateBookmarkAsync(
        BookmarkEntry? bookmark,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (bookmark is null)
        {
            return Current;
        }
        if (Interlocked.CompareExchange(ref _bookmarkNavigationInProgress, 1, 0) != 0)
        {
            return Current;
        }
        PaneSide side = _panes.Current.ActiveSide;
        try
        {
            DualPaneSnapshot result = await _panes.HandleAsync(
                new ResolvedBookmarkNavigation(bookmark.Path.Value),
                observer,
                cancellationToken).ConfigureAwait(false);
            PaneSnapshot pane = result.Of(side);
            if (_settings.Current.Editor == SettingsEditorState.Bookmarks &&
                pane.Activity == PaneActivity.Idle &&
                pane.Content is PaneContentListed listed &&
                FileSystemPathIdentityComparer.Instance.Equals(
                    listed.Listing.Location,
                    bookmark.Path.Value))
            {
                _ = _settings.Close();
            }
            return Current;
        }
        finally
        {
            Volatile.Write(ref _bookmarkNavigationInProgress, 0);
        }
    }

    private bool PaneInteractionIsFrozen()
    {
        return _panes.Current.Operation is
            OperationRunning or OperationAwaitingConfirmation or OperationAwaitingName or
            OperationAwaitingConflict;
    }

    private bool BookmarkInteractionIsFrozen()
    {
        DualPaneSnapshot panes = _panes.Current;
        return PaneInteractionIsFrozen() || panes.Left.Activity is PaneLoading ||
            panes.Right.Activity is PaneLoading;
    }
}
