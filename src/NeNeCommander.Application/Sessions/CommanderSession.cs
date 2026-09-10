using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Commands;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Coordinates the existing dual-pane session with the session-owned settings, bookmark, address,
/// and command-palette interactions. Each inner session remains the sole owner of its state; this
/// coordinator chooses which one receives an intent and freezes lower-precedence work.
/// </summary>
public sealed class CommanderSession
{
    private readonly DualPaneSession _panes;
    private readonly SettingsSession _settings;
    private AddressEditorState _addressEditor;
    private CommandPaletteState _commandPalette;
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
        _addressEditor = AddressEditorState.Closed;
        _commandPalette = CommandPaletteState.Closed;
    }

    /// <summary>Gets the current complete application-session snapshot.</summary>
    public CommanderSnapshot Current => new(_panes.Current, _settings.Current, _addressEditor, _commandPalette);

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
            _addressEditor is not AddressEditorClosed ||
            _commandPalette is CommandPaletteOpen)
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
        if (_commandPalette is CommandPaletteOpen openPalette)
        {
            return await HandlePaletteIntentAsync(openPalette, intent, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        if (Volatile.Read(ref _bookmarkNavigationInProgress) != 0)
        {
            return Current;
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
            : _addressEditor is not AddressEditorClosed
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

    private void OpenCommandPalette()
    {
        DualPaneSnapshot panes = _panes.Current;
        if (PaneInteractionIsFrozen() ||
            AnyPaneExternalWorkIsRunning(panes) ||
            panes.Of(panes.ActiveSide).Content is not PaneContentListed)
        {
            return;
        }
        _commandPalette = new CommandPaletteOpen(
            panes.Left,
            panes.Right,
            panes.ActiveSide,
            CommandCatalog.Capture(panes));
    }

    private async Task<CommanderSnapshot> HandlePaletteIntentAsync(
        CommandPaletteOpen open,
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        DualPaneSnapshot current = _panes.Current;
        if (intent is CommandPaletteCancellation cancellation)
        {
            if (!ReferenceEquals(open, cancellation.ExpectedState))
            {
                return Current;
            }
            _commandPalette = PaletteScopeOwnsInput(open, current)
                ? CommandPaletteState.CloseFocusing(open.ActiveSide)
                : CommandPaletteState.Closed;
            return Current;
        }
        if (intent is not CommandPaletteSubmission submission ||
            !ReferenceEquals(open, submission.ExpectedState) ||
            !CommandCatalog.Contains(submission.SelectedIntent))
        {
            return Current;
        }

        if (!PaletteScopeOwnsInput(open, current))
        {
            _commandPalette = CommandPaletteState.Closed;
            return Current;
        }
        CommandCandidate candidate = open.Candidates.First(candidate =>
            candidate.Intent == submission.SelectedIntent);
        if (candidate.Availability != CommandAvailability.Available)
        {
            return Current;
        }

        _commandPalette = CommandPaletteState.Closed;
        return await DispatchIdleIntentAsync(submission.SelectedIntent, observer, cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool PaletteScopeIsCurrent(CommandPaletteOpen open, DualPaneSnapshot current)
    {
        return current.ActiveSide == open.ActiveSide &&
            ReferenceEquals(current.Left, open.Left) &&
            ReferenceEquals(current.Right, open.Right);
    }

    private bool PaletteScopeOwnsInput(CommandPaletteOpen open, DualPaneSnapshot current)
    {
        return PaletteScopeIsCurrent(open, current) &&
            _settings.Current.Editor == SettingsEditorState.Closed &&
            _addressEditor is AddressEditorClosed &&
            !PaneInteractionIsFrozen() &&
            !AnyPaneExternalWorkIsRunning(current);
    }

    private static bool AnyPaneExternalWorkIsRunning(DualPaneSnapshot panes)
    {
        return panes.Left.Activity is PaneLoading or PaneLaunching ||
            panes.Right.Activity is PaneLoading or PaneLaunching;
    }

    private async Task<CommanderSnapshot> HandleAddressIntentAsync(
        UserIntent intent,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (intent == UserIntent.FocusAddress)
        {
            return Current;
        }
        if (intent is AddressFocusSubmission focusedAddress)
        {
            return await BeginAddressEditAsync(focusedAddress.Side, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        if (intent == UserIntent.Escape)
        {
            PaneSide side = EditorSide(_addressEditor);
            _addressEditor = AddressEditorState.CloseFocusing(side);
            return Current;
        }
        if (intent is AddressFocusDeparture departure)
        {
            if (ReferenceEquals(_addressEditor, departure.ExpectedState))
            {
                _addressEditor = AddressEditorState.Closed;
            }
            return Current;
        }
        return intent is AddressSubmission submission
            ? await SubmitAddressAsync(submission, cancellationToken).ConfigureAwait(false)
            : Current;
    }

    private async Task<CommanderSnapshot> BeginAddressEditAsync(
        PaneSide side,
        ICommanderProgressObserver observer,
        CancellationToken cancellationToken)
    {
        if (AddressSide(_addressEditor) == side)
        {
            return Current;
        }
        if (PaneInteractionIsFrozen() || AnyPaneReadIsRunning())
        {
            return Current;
        }
        DualPaneSnapshot panes = _panes.Current;
        PaneSnapshot requested = SnapshotOf(panes, side);
        if (requested.Content is not PaneContentListed listed)
        {
            return Current;
        }
        if (panes.ActiveSide != side)
        {
            _ = await _panes.HandleAsync(UserIntent.ActivateOtherPane, observer, cancellationToken)
                .ConfigureAwait(false);
        }
        _addressEditor = new AddressEditing(side, listed.Listing.Location);
        return Current;
    }

    private async Task<CommanderSnapshot> SubmitAddressAsync(
        AddressSubmission submission,
        CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(_addressEditor, submission.ExpectedState) ||
            PaneInteractionIsFrozen() ||
            AnyPaneReadIsRunning())
        {
            return Current;
        }
        PaneSide side = EditorSide(_addressEditor);
        FileSystemPath original = EditorOriginalLocation(_addressEditor);
        PathParseOutcome outcome = FileSystemPath.Parse(submission.RawText);
        if (outcome is PathParseFailure failure)
        {
            _addressEditor = new AddressInputRejected(side, original, submission.RawText, failure.Kind);
            return Current;
        }
        FileSystemPath target = ((PathParseSuccess)outcome).Path;
        _addressEditor = AddressEditorState.CloseFocusing(side);
        _ = await _panes.NavigateAsync(side, target, cancellationToken).ConfigureAwait(false);
        return Current;
    }

    private bool AnyPaneReadIsRunning()
    {
        DualPaneSnapshot panes = _panes.Current;
        return panes.Left.Activity is PaneLoading || panes.Right.Activity is PaneLoading;
    }

    private static PaneSnapshot SnapshotOf(DualPaneSnapshot panes, PaneSide side)
    {
        return side == PaneSide.Left ? panes.Left : panes.Right;
    }

    private static PaneSide? AddressSide(AddressEditorState state)
    {
        return state switch
        {
            AddressEditing editing => editing.Side,
            AddressInputRejected rejected => rejected.Side,
            _ => null,
        };
    }

    private static PaneSide EditorSide(AddressEditorState state)
    {
        return state is AddressEditing editing ? editing.Side : ((AddressInputRejected)state).Side;
    }

    private static FileSystemPath EditorOriginalLocation(AddressEditorState state)
    {
        return state is AddressEditing editing
            ? editing.OriginalLocation
            : ((AddressInputRejected)state).OriginalLocation;
    }
}
