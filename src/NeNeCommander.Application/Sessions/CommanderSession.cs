using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Coordinates the existing dual-pane session with the session-owned settings, bookmark, and
/// transient scope interactions. Each owner remains the sole owner of its state; this coordinator
/// chooses which one receives an intent, freezes lower-precedence work, and performs every effect.
/// </summary>
public sealed class CommanderSession
{
    private readonly DualPaneSession _panes;
    private readonly SettingsSession _settings;
    private readonly TransientScopeOwners _scopes;
    private int _bookmarkNavigationInProgress;

    /// <summary>Initializes the application session over its declared state owners.</summary>
    /// <param name="panes">Sole dual-pane coordinator.</param>
    /// <param name="settings">Sole settings interaction owner.</param>
    /// <param name="scopes">Sole owners of the transient scopes.</param>
    public CommanderSession(DualPaneSession panes, SettingsSession settings, TransientScopeOwners scopes)
    {
        ArgumentNullException.ThrowIfNull(panes);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scopes);
        _panes = panes;
        _settings = settings;
        _scopes = scopes;
    }

    /// <summary>Gets the current complete application-session snapshot.</summary>
    public CommanderSnapshot Current
    {
        get
        {
            TransientScopeSnapshot scopes = new(
                _scopes.AddressEditor.Current,
                _scopes.CommandPalette.Current,
                _scopes.WindowAdjustment.Current);
            return new CommanderSnapshot(_panes.Current, _settings.Current, scopes);
        }
    }

    /// <summary>Reads one pane location unless another interaction owns modal input.</summary>
    public async Task<CommanderSnapshot> NavigateAsync(
        PaneSide side,
        FileSystemPath location,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(location);
        if (Volatile.Read(ref _bookmarkNavigationInProgress) != 0 ||
            _settings.Current.Editor != SettingsEditorState.Closed ||
            _scopes.AddressEditor.Current is not AddressEditorClosed ||
            _scopes.CommandPalette.Current is CommandPaletteOpen ||
            _scopes.WindowAdjustment.Current is WindowAdjustmentOpen)
        {
            return Current;
        }
        _ = await _panes.NavigateAsync(side, location, cancellationToken).ConfigureAwait(false);
        return Current;
    }

    /// <summary>Routes one typed intent to the single interaction owner in effect.</summary>
    public async Task<CommanderSnapshot> HandleAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(observer);
        if (_scopes.WindowAdjustment.Current is WindowAdjustmentOpen)
        {
            return Current;
        }
        if (_scopes.CommandPalette.Current is CommandPaletteOpen)
        {
            return await HandlePaletteIntentAsync(intent, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        SettingsEditorState editor = _settings.Current.Editor;
        if (editor == SettingsEditorState.Open)
        {
            HandleSettingsIntent(intent, observer);
            return Current;
        }
        return editor == SettingsEditorState.Bookmarks
            ? await HandleBookmarksModalIntentAsync(intent, observer, cancellationToken)
                .ConfigureAwait(false)
            : _scopes.AddressEditor.Current is not AddressEditorClosed
                ? await HandleAddressIntentAsync(intent, observer, cancellationToken).ConfigureAwait(false)
                : await DispatchIdleIntentAsync(intent, observer, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CommanderSnapshot> HandleBookmarksModalIntentAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        return intent is BookmarkNavigationSelection managerNavigation
            ? await NavigateManagerBookmarkAsync(
                managerNavigation.Selection,
                observer,
                cancellationToken).ConfigureAwait(false)
            : HandleBookmarkIntent(intent, observer, cancellationToken);
    }

    private async Task<CommanderSnapshot> DispatchIdleIntentAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (intent is BookmarkShortcutSelection shortcut)
        {
            BookmarkEntry? bookmark = _settings.Current.Settings.Bookmarks.Find(shortcut.Slot);
            return bookmark is null
                ? Current
                : await NavigateDirectBookmarkAsync(bookmark, observer, cancellationToken)
                    .ConfigureAwait(false);
        }
        if (Volatile.Read(ref _bookmarkNavigationInProgress) != 0)
        {
            return Current;
        }
        if (intent == UserIntent.FocusAddress)
        {
            return await BeginAddressEditAsync(_panes.Current.ActiveSide, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        if (intent is AddressFocusSubmission focusedAddress)
        {
            return await BeginAddressEditAsync(focusedAddress.Side, observer, cancellationToken)
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
        if (intent == UserIntent.OpenCommandPalette)
        {
            OpenCommandPalette();
            return Current;
        }
        if (intent == UserIntent.OpenWindowAdjustment)
        {
            OpenWindowAdjustment();
            return Current;
        }
        _ = await _panes.HandleAsync(intent, observer, cancellationToken).ConfigureAwait(false);
        return Current;
    }

    /// <summary>
    /// Decides one qualified window adjustment and returns the plan the host applies exactly once.
    /// It is the mode's declared synchronous input route: it awaits nothing, allocates no
    /// asynchronous work, and reads and writes only the window-adjustment scope, so it stays
    /// correct on every key-repeat event while a pane read is pending.
    /// </summary>
    /// <param name="request">Action qualified by the expected open state and a fresh placement.</param>
    /// <returns>The decided plan, or nothing to apply for a stale or closed mode.</returns>
    public WindowAdjustmentDecision AdjustWindow(WindowAdjustmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _scopes.WindowAdjustment.Adjust(request);
    }

    /// <summary>
    /// Leaves one qualified open window-adjustment mode and requests the one-time focus return. It
    /// reads no placement and cannot be refused, so the mode never traps the user.
    /// </summary>
    /// <param name="expected">Exact open state the caller last rendered.</param>
    /// <returns>The window-adjustment state current after this decision.</returns>
    public WindowAdjustmentState LeaveWindowAdjustment(WindowAdjustmentOpen expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        return _scopes.WindowAdjustment.Leave(expected);
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

    private CommanderSnapshot HandleBookmarkIntent(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (intent == UserIntent.Escape)
        {
            _ = _settings.ApplyBookmarkEditorAction(
                BookmarkEditorAction.Cancel,
                CurrentBookmarkDefaults(),
                observer,
                cancellationToken);
            return Current;
        }
        if (intent is BookmarkEditorActionSubmission submission)
        {
            _ = _settings.ApplyBookmarkEditorAction(
                submission.Action,
                CurrentBookmarkDefaults(),
                observer,
                cancellationToken);
            return Current;
        }
        return Current;
    }

    private async Task<CommanderSnapshot> NavigateDirectBookmarkAsync(
        BookmarkEntry bookmark,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (!TryBeginBookmarkNavigation())
        {
            return Current;
        }
        try
        {
            if (BookmarkInteractionIsFrozen())
            {
                return Current;
            }
            _ = await _panes.HandleAsync(
                new ResolvedBookmarkNavigation(bookmark.Path.Value),
                observer,
                cancellationToken).ConfigureAwait(false);
            return Current;
        }
        finally
        {
            Volatile.Write(ref _bookmarkNavigationInProgress, 0);
        }
    }

    private async Task<CommanderSnapshot> NavigateManagerBookmarkAsync(
        BookmarkSelection selection,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (!TryBeginBookmarkNavigation())
        {
            return Current;
        }
        FileSystemPath navigationTarget = selection.Entry.Path.Value;
        try
        {
            BookmarkNavigationStart start = _settings.BeginBookmarkNavigation(selection);
            if (start is not BookmarkNavigationStart.Accepted accepted)
            {
                return Current;
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
            return Current;
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

    private BookmarkRegistrationDefaults CurrentBookmarkDefaults()
    {
        DualPaneSnapshot panes = _panes.Current;
        PaneSnapshot pane = panes.Of(panes.ActiveSide);
        if (pane.Content is not PaneContentListed listed)
        {
            return new BookmarkRegistrationDefaults(string.Empty, string.Empty);
        }
        string path = listed.Listing.Location.CanonicalText;
        string candidate = LeafName(path);
        string name = BookmarkDisplayName.Parse(candidate) is BookmarkDisplayNameAccepted accepted
            ? accepted.Name.Value
            : string.Empty;
        return new BookmarkRegistrationDefaults(name, path);
    }

    private static string LeafName(string path)
    {
        string withoutTrailingSeparator = path.TrimEnd('\\');
        int separator = withoutTrailingSeparator.LastIndexOf('\\');
        return separator < 0 ? string.Empty : withoutTrailingSeparator[(separator + 1)..];
    }

    private bool PaneInteractionIsFrozen()
    {
        return _panes.Current.Operation is
            OperationRunning or OperationAwaitingConfirmation or OperationAwaitingName or
            OperationAwaitingConflict;
    }

    private bool BookmarkInteractionIsFrozen()
    {
        return PaneInteractionIsFrozen() || AnyPaneExternalWorkIsRunning(_panes.Current);
    }

    private static bool AnyPaneExternalWorkIsRunning(DualPaneSnapshot panes)
    {
        return panes.Left.Activity is PaneLoading or PaneLaunching ||
            panes.Right.Activity is PaneLoading or PaneLaunching;
    }

    private bool AnyPaneReadIsRunning()
    {
        DualPaneSnapshot panes = _panes.Current;
        return panes.Left.Activity is PaneLoading || panes.Right.Activity is PaneLoading;
    }

    private void OpenCommandPalette()
    {
        DualPaneSnapshot panes = _panes.Current;
        _ = _scopes.CommandPalette.Open(panes, PaletteOwnership(panes));
    }

    private void OpenWindowAdjustment()
    {
        DualPaneSnapshot panes = _panes.Current;
        _ = _scopes.WindowAdjustment.Open(panes.ActiveSide, WindowOwnership(panes));
    }

    /// <summary>
    /// Derives window-mode ownership from the palette scope and the shared idle rule the palette
    /// already uses. Listed pane content is not part of the rule: a pane whose last read failed
    /// does not prevent moving the window.
    /// </summary>
    private InteractionOwnership WindowOwnership(DualPaneSnapshot panes)
    {
        return _scopes.CommandPalette.Current is CommandPaletteOpen
            ? InteractionOwnership.AnotherScopeOwnsInput
            : PaletteOwnership(panes);
    }

    private async Task<CommanderSnapshot> HandlePaletteIntentAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        DualPaneSnapshot panes = _panes.Current;
        CommandPaletteValidation validation = _scopes.CommandPalette.Validate(
            intent,
            panes,
            PaletteOwnership(panes));
        return validation is CommandPaletteIntentAccepted accepted
            ? await DispatchIdleIntentAsync(accepted.Intent, observer, cancellationToken)
                .ConfigureAwait(false)
            : Current;
    }

    private InteractionOwnership PaletteOwnership(DualPaneSnapshot panes)
    {
        return _settings.Current.Editor == SettingsEditorState.Closed &&
            _scopes.AddressEditor.Current is AddressEditorClosed &&
            !PaneInteractionIsFrozen() &&
            !AnyPaneExternalWorkIsRunning(panes)
                ? InteractionOwnership.ScopeOwnsInput
                : InteractionOwnership.AnotherScopeOwnsInput;
    }

    private async Task<CommanderSnapshot> HandleAddressIntentAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (intent is AddressFocusSubmission focusedAddress)
        {
            return await BeginAddressEditAsync(focusedAddress.Side, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        if (_scopes.AddressEditor.Validate(intent, AddressOwnership()) is not AddressTargetAccepted accepted)
        {
            return Current;
        }
        _ = await _panes.NavigateAsync(accepted.Side, accepted.Target, cancellationToken)
            .ConfigureAwait(false);
        return Current;
    }

    private async Task<CommanderSnapshot> BeginAddressEditAsync(
        PaneSide side,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        DualPaneSnapshot panes = _panes.Current;
        if (_scopes.AddressEditor.Admit(panes, side, AddressOwnership()) is not AddressEditAdmitted admitted)
        {
            return Current;
        }
        if (panes.ActiveSide != side)
        {
            _ = await _panes.HandleAsync(UserIntent.ActivateOtherPane, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        _ = _scopes.AddressEditor.Open(admitted);
        return Current;
    }

    private InteractionOwnership AddressOwnership()
    {
        return PaneInteractionIsFrozen() || AnyPaneReadIsRunning()
            ? InteractionOwnership.AnotherScopeOwnsInput
            : InteractionOwnership.ScopeOwnsInput;
    }
}
