using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Windowing;
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
    public CommanderSnapshot Current => new(_panes.Current, _settings.Current, _scopes.Capture());

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
            _scopes.WindowAdjustment.Current is WindowAdjustmentOpen ||
            _scopes.KeyboardHelp.Current is KeyboardHelpOpen ||
            _scopes.Locations.Current is not LocationsClosed)
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
        if (CommandPaletteSession.IsContextRequest(intent))
        {
            return OpenContextMenu(intent);
        }
        if (_scopes.WindowAdjustment.Current is WindowAdjustmentOpen ||
            _scopes.KeyboardHelp.Current is KeyboardHelpOpen)
        {
            return Current;
        }
        if (_scopes.Locations.Current is not LocationsClosed)
        {
            return await HandleLocationsIntentAsync(intent, cancellationToken).ConfigureAwait(false);
        }
        if (_scopes.CommandPalette.Current is CommandPaletteOpen)
        {
            return await HandlePaletteIntentAsync(intent, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        SettingsEditorState editor = _settings.Current.Editor;
        if (editor == SettingsEditorState.Open || editor is ColorSchemePickerOpen)
        {
            _ = _settings.HandleEditorIntentAsync(intent, observer, CancellationToken.None);
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
        if (Volatile.Read(ref _bookmarkNavigationInProgress) != 0 && intent != UserIntent.Escape)
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
        if (intent == UserIntent.OpenLocations)
        {
            DualPaneSnapshot panes = _panes.Current;
            _ = await _scopes.Locations.OpenAsync(panes, PaletteOwnership(panes), cancellationToken)
                .ConfigureAwait(false);
            return Current;
        }
        return OpenScope(intent)
            ? Current
            : await DispatchPaneIntentAsync(intent, observer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens the scope an intent names without waiting for anything, and reports whether the intent
    /// named one. A frozen interaction leaves the settings editor and the bookmark manager closed.
    /// </summary>
    private bool OpenScope(UserIntent intent)
    {
        DualPaneSnapshot panes = _panes.Current;
        InteractionOwnership ownership = PaletteOwnership(panes);
        Action? open = intent switch
        {
            _ when intent == UserIntent.OpenSettings => OpenSettingsEditor,
            _ when intent == UserIntent.OpenColorSchemes => OpenColorSchemePicker,
            _ when intent == UserIntent.OpenBookmarks => OpenBookmarkManager,
            _ when intent == UserIntent.OpenCommandPalette => () => _scopes.CommandPalette.Open(panes, CommandSurface.Palette, ownership),
            _ when intent == UserIntent.OpenWindowAdjustment => () => _scopes.WindowAdjustment.Open(panes.ActiveSide, ownership),
            _ when intent == UserIntent.OpenHelp => () => _scopes.KeyboardHelp.Open(panes.ActiveSide, ownership),
            _ => null,
        };
        open?.Invoke();
        return open is not null;
    }

    private void OpenSettingsEditor()
    {
        if (!PaneInteractionIsFrozen())
        {
            _ = _settings.Open();
        }
    }

    private void OpenColorSchemePicker()
    {
        if (!PaneInteractionIsFrozen())
        {
            _settings.OpenColorSchemes();
        }
    }

    private void OpenBookmarkManager()
    {
        if (!BookmarkInteractionIsFrozen())
        {
            _ = _settings.OpenBookmarks();
        }
    }

    private async Task<CommanderSnapshot> DispatchPaneIntentAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        DualPaneSnapshot before = _panes.Current;
        PaneSide side = before.ActiveSide;
        DualPaneSnapshot after = await _panes.HandleAsync(intent, observer, cancellationToken).ConfigureAwait(false);
        if (PaneReducer.SortKeyRequestedBy(intent) is not null &&
            before.Of(side).Content is PaneContentListed previous &&
            after.Of(side).Content is PaneContentListed current &&
            previous.State.SortOrder != current.State.SortOrder)
        {
            _ = _settings.SavePaneSortOrderAsync(side, current.State.SortOrder, observer, CancellationToken.None);
        }
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

    /// <summary>Records the effect of one qualified transfer through the existing window scope owner.</summary>
    /// <param name="expected">Exact decided owner returned with the transfer plan.</param>
    /// <param name="effect">Observation made by the sole window adapter.</param>
    /// <returns>The current window-adjustment state; a stale receipt changes nothing.</returns>
    public WindowAdjustmentState ReportWindowTransfer(WindowAdjustmentOpen expected, WindowTransferEffect effect)
    {
        return _scopes.WindowAdjustment.ReportTransfer(expected, effect);
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

    /// <summary>
    /// Closes one qualified open keyboard help surface and requests the one-time focus return. It is
    /// the help's declared synchronous route: it awaits nothing and touches only the help scope, so a
    /// close is never refused while a pane read is pending (ADR-0076).
    /// </summary>
    /// <param name="expected">Exact open state the caller last rendered.</param>
    /// <returns>The keyboard help state current after this decision.</returns>
    public KeyboardHelpState CloseKeyboardHelp(KeyboardHelpOpen expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        return _scopes.KeyboardHelp.Close(expected);
    }

    /// <summary>Queues a changed final placement and always drains the existing settings queue.</summary>
    /// <param name="placement">Last normal placement, or absence when none could be captured.</param>
    /// <param name="observer">Receives the existing typed persistence outcome.</param>
    public async Task StopAsync(SavedWindowPlacement? placement, ISettingsProgressObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        try
        {
            await _settings.SaveWindowPlacementAsync(placement, observer, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            await _settings.StopAsync().ConfigureAwait(false);
        }
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
                ActivePaneBookmarkDefaults.From(_panes.Current),
                observer,
                cancellationToken);
            return Current;
        }
        if (intent is BookmarkEditorActionSubmission submission)
        {
            _ = _settings.ApplyBookmarkEditorAction(
                submission.Action,
                ActivePaneBookmarkDefaults.From(_panes.Current),
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

    private CommanderSnapshot OpenContextMenu(UserIntent intent)
    {
        DualPaneSnapshot before = _panes.Current;
        if (!CanAcquireContext(before) || CommandContextRequest.Create(intent, before) is not CommandContextRequest request)
        {
            return Current;
        }
        DualPaneSnapshot after = _panes.ApplyInteraction(request.Interaction);
        if (!ReferenceEquals(before.Of(request.Interaction.Side), after.Of(request.Interaction.Side)))
        {
            _ = _scopes.CommandPalette.Open(after, request.Surface, InteractionOwnership.ScopeOwnsInput);
        }
        return Current;
    }

    private bool CanAcquireContext(DualPaneSnapshot panes)
    {
        InteractionOwnership ownership = PaletteOwnership(panes);
        return ownership == InteractionOwnership.ScopeOwnsInput &&
            Volatile.Read(ref _bookmarkNavigationInProgress) == 0 &&
            _scopes.WindowAdjustment.Current is not WindowAdjustmentOpen &&
            _scopes.KeyboardHelp.Current is not KeyboardHelpOpen &&
            _scopes.Locations.Current is LocationsClosed &&
            _scopes.CommandPalette.CanAcquireContext(panes, ownership);
    }

    /// <summary>
    /// Routes one intent to the open or loading Locations picker and performs the one navigation an
    /// accepted selection names, after the picker has already closed.
    /// </summary>
    private async Task<CommanderSnapshot> HandleLocationsIntentAsync(
        UserIntent intent,
        CancellationToken cancellationToken)
    {
        LocationsValidation validation = _scopes.Locations.Validate(intent, PaletteOwnership(_panes.Current));
        if (validation is LocationTargetAccepted accepted)
        {
            _ = await _panes.NavigateAsync(accepted.Side, accepted.Target, cancellationToken)
                .ConfigureAwait(false);
        }
        return Current;
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
