using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>Owns existing bookmark navigation coordination and its latch for the sole commander dispatcher.</summary>
public sealed class CommanderBookmarkCommands
{
    private readonly DualPaneSession _panes;
    private readonly SettingsSession _settings;
    private int _bookmarkNavigationInProgress;

    /// <summary>Receives the same pane and settings owners supplied to CommanderSession.</summary>
    public CommanderBookmarkCommands(DualPaneSession panes, SettingsSession settings)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(settings);
        _panes = panes;
        _settings = settings;
    }

    internal bool IsNavigating => Volatile.Read(ref _bookmarkNavigationInProgress) != 0;

    internal bool IsInteractionFrozen => !_panes.Current.CanInteractWithPanes;

    internal bool MatchesOwners(DualPaneSession panes, SettingsSession settings)
    {
        return ReferenceEquals(_panes, panes) && ReferenceEquals(_settings, settings);
    }

    internal async Task NavigateDirectAsync(
        BookmarkEntry bookmark,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (!TryBeginBookmarkNavigation())
        {
            return;
        }
        try
        {
            if (IsInteractionFrozen)
            {
                return;
            }
            _ = await _panes.HandleAsync(
                new ResolvedBookmarkNavigation(bookmark.Path.Value),
                observer,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _bookmarkNavigationInProgress, 0);
        }
    }

    internal async Task NavigateManagerAsync(
        BookmarkSelection selection,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (!TryBeginBookmarkNavigation())
        {
            return;
        }
        FileSystemPath navigationTarget = selection.Entry.Path.Value;
        try
        {
            BookmarkNavigationStart start = _settings.BeginBookmarkNavigation(selection);
            if (start is not BookmarkNavigationStart.Accepted accepted)
            {
                return;
            }
            navigationTarget = accepted.Entry.Path.Value;
            PaneSide side = _panes.Current.ActiveSide;
            DualPaneSnapshot result = await _panes.HandleAsync(
                new ResolvedBookmarkNavigation(navigationTarget),
                observer,
                cancellationToken).ConfigureAwait(false);
            PaneSnapshot pane = result.Of(side);
            bool succeeded = pane.Activity == PaneActivity.Idle &&
                pane.Content is PaneContentListed listed &&
                FileSystemPathIdentityComparer.Instance.Equals(
                    listed.Listing.Location,
                    navigationTarget);
            if (succeeded)
            {
                _settings.FinishBookmarkNavigationSucceeded();
            }
            else
            {
                _settings.FinishBookmarkNavigationFailed(
                    BookmarkNavigationFailure(pane.Activity));
            }
            return;
        }
        catch (OperationCanceledException)
        {
            _settings.FinishBookmarkNavigationFailed(
                new PaneReadCancelled(navigationTarget));
            throw;
        }
        finally
        {
            Volatile.Write(ref _bookmarkNavigationInProgress, 0);
        }
    }

    private static PaneActivity BookmarkNavigationFailure(PaneActivity activity)
    {
        return activity is PaneReadCancelled ? activity : (PaneReadFailed)activity;
    }

    private bool TryBeginBookmarkNavigation()
    {
        return Interlocked.CompareExchange(ref _bookmarkNavigationInProgress, 1, 0) == 0;
    }

}
