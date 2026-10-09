using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Launching;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Coordinates one pane: it owns the current <see cref="PaneSnapshot"/>, routes focus and
/// selection intents through <see cref="PaneReducer"/>, performs location changes through the
/// sole directory read port, and hands focused files to the sole launcher boundary. Each read owns
/// one token source linked to the caller's token; <see cref="UserIntent.Escape"/> abandons the
/// read in flight through that source and the existing supersession check (ADR-0058). It is not
/// thread-safe and is driven from one owner.
/// </summary>
public sealed class PaneSession
{
    private static readonly Action CancelNothingAction = CancelNothing;

    private readonly int _entryBoundary;
    private readonly HiddenItemVisibility _initialHiddenItemVisibility;
    private readonly IFileLauncher _fileLauncher;
    private readonly IDirectoryReadPort _port;
    private readonly VisiblePageCapacity _visiblePageCapacity;
    private object? _latestNavigation;
    private Action _cancelLatestRead = CancelNothingAction;

    /// <summary>Initializes an empty session over one read port.</summary>
    /// <param name="port">Provider-neutral directory read port.</param>
    /// <param name="fileLauncher">Sole provider boundary for a focused file handoff.</param>
    /// <param name="visiblePageCapacity">Validated visible-row capacity used for paging.</param>
    /// <param name="entryBoundary">Entry boundary applied to every read, within the fixed range.</param>
    /// <param name="hiddenItemVisibility">
    /// Visibility the first listing uses. It is a starting value, not an owner: once a location is
    /// listed the pane state owns the visibility and every later read carries the state's value.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The boundary is outside the fixed range, which is a composition defect.</exception>
    public PaneSession(
        IDirectoryReadPort port,
        IFileLauncher fileLauncher,
        VisiblePageCapacity visiblePageCapacity,
        int entryBoundary,
        HiddenItemVisibility hiddenItemVisibility)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(fileLauncher);
        ArgumentNullException.ThrowIfNull(visiblePageCapacity);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);
        if (!DirectoryReadRequest.IsValidEntryBoundary(entryBoundary))
        {
            throw new ArgumentOutOfRangeException(nameof(entryBoundary));
        }
        _port = port;
        _fileLauncher = fileLauncher;
        _visiblePageCapacity = visiblePageCapacity;
        _entryBoundary = entryBoundary;
        _initialHiddenItemVisibility = hiddenItemVisibility;
        Current = PaneSnapshot.Initial;
    }

    /// <summary>Gets the current immutable snapshot.</summary>
    public PaneSnapshot Current { get; private set; }

    /// <summary>
    /// Reads a location and, on success, replaces the content with focus on the first entry.
    /// A newer navigation supersedes this one: a superseded result is discarded. A file handoff
    /// in flight freezes this entry point and returns the current snapshot without a read.
    /// </summary>
    /// <param name="location">Validated location to read.</param>
    /// <param name="cancellationToken">Token observed by the read.</param>
    /// <returns>The snapshot current after the read completed or was superseded.</returns>
    public Task<PaneSnapshot> NavigateAsync(FileSystemPath location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        return Current.Activity is PaneLaunching
            ? Task.FromResult(Current)
            : NavigateAsync(location, null, PaneNavigationAction.Append, cancellationToken);
    }

    /// <summary>
    /// Re-reads the listed location, keeping the current focus item when it still exists and
    /// clearing selection. Nothing happens before the first listing or while a read or file
    /// handoff is in flight.
    /// </summary>
    /// <param name="cancellationToken">Token observed by the read.</param>
    /// <returns>The snapshot current after the read completed or was superseded.</returns>
    public Task<PaneSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        return Current.Content is PaneContentListed listed
            ? RefreshListedAsync(listed, listed.State.FocusItem, cancellationToken)
            : Task.FromResult(Current);
    }

    /// <summary>
    /// Re-reads the listed location, focusing the given item when the new listing contains it and
    /// clearing selection. Nothing happens before the first listing or while a read or file
    /// handoff is in flight.
    /// </summary>
    /// <param name="preferredFocus">Item to focus after the read, typically one the session just created.</param>
    /// <param name="cancellationToken">Token observed by the read.</param>
    /// <returns>The snapshot current after the read completed or was superseded.</returns>
    public Task<PaneSnapshot> RefreshFocusingAsync(FileSystemPath preferredFocus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preferredFocus);
        return Current.Content is PaneContentListed listed
            ? RefreshListedAsync(listed, preferredFocus, cancellationToken)
            : Task.FromResult(Current);
    }

    private Task<PaneSnapshot> RefreshListedAsync(
        PaneContentListed listed,
        FileSystemPath? preferredFocus,
        CancellationToken cancellationToken)
    {
        return Current.Activity is PaneLoading or PaneLaunching
            ? Task.FromResult(Current)
            : NavigateAsync(
                listed.State.Location,
                preferredFocus,
                PaneNavigationAction.Preserve,
                cancellationToken);
    }

    /// <summary>
    /// Applies one intent. Movement and selection use the reducer; opening a directory starts a
    /// read, opening a file starts one provider handoff, and refresh re-reads the current location.
    /// Intents are frozen while either external action is in flight, except that
    /// <see cref="UserIntent.Escape"/> abandons a read in flight: the read is superseded, its token
    /// is cancelled, and the pane keeps its previous content under <see cref="PaneReadAbandoned"/>.
    /// </summary>
    /// <param name="intent">Typed user intent.</param>
    /// <param name="cancellationToken">Token observed by any read or file handoff the intent starts.</param>
    /// <returns>The resulting snapshot.</returns>
    public Task<PaneSnapshot> HandleAsync(UserIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (Current.Activity is PaneLoading loading)
        {
            return Task.FromResult(intent == UserIntent.Escape ? AbandonRead(loading) : Current);
        }
        if (Current.Activity is PaneLaunching ||
            Current.Content is not PaneContentListed listed)
        {
            return Task.FromResult(Current);
        }
        if (intent == UserIntent.OpenFocused)
        {
            return OpenFocusedAsync(listed, cancellationToken);
        }
        if (intent == UserIntent.Refresh)
        {
            return NavigateAsync(
                listed.State.Location,
                listed.State.FocusItem,
                PaneNavigationAction.Preserve,
                cancellationToken);
        }
        if (intent == UserIntent.NavigateParent)
        {
            FileSystemPath? parent = listed.State.Location.Parent;
            return parent is null
                ? Task.FromResult(Current)
                : NavigateAsync(
                    parent,
                    listed.State.Location,
                    PaneNavigationAction.Append,
                    cancellationToken);
        }
        if (intent == UserIntent.NavigateBack)
        {
            return NavigateHistoryAsync(listed, PaneNavigationAction.Back, cancellationToken);
        }
        if (intent == UserIntent.NavigateForward)
        {
            return NavigateHistoryAsync(listed, PaneNavigationAction.Forward, cancellationToken);
        }

        PaneState next = PaneReducer.Apply(listed.State, intent);
        if (!ReferenceEquals(next, listed.State))
        {
            Current = PaneSnapshot.IdleWith(new PaneContentListed(next, listed.Listing));
        }
        return Task.FromResult(Current);
    }

    private Task<PaneSnapshot> OpenFocusedAsync(PaneContentListed listed, CancellationToken cancellationToken)
    {
        DirectoryEntry? focused = listed.FindFocusedEntry();
        return focused is null
            ? Task.FromResult(Current)
            : focused.Kind == DirectoryEntryKind.Directory
                ? NavigateAsync(focused.Path, null, PaneNavigationAction.Append, cancellationToken)
                : focused.Path is WindowsLocalPath local
                    ? LaunchAsync(local, cancellationToken)
                    : Task.FromResult(RejectUnsupportedLaunch(focused.Path));
    }

    private async Task<PaneSnapshot> LaunchAsync(
        WindowsLocalPath target,
        CancellationToken cancellationToken)
    {
        PaneContent content = Current.Content;
        Current = Current.WithActivity(new PaneLaunching(target));
        FileLaunchOutcome outcome = await _fileLauncher.LaunchAsync(target, cancellationToken);
        Current = outcome switch
        {
            FileLaunchAccepted => PaneSnapshot.IdleWith(content),
            FileLaunchCancelled => Current.WithActivity(new PaneLaunchCancelled(target)),
            FileLaunchFailed failed => Current.WithActivity(new PaneLaunchFailed(target, failed.Failure)),
            _ => throw new InvalidOperationException("The file launch outcome variant is not supported."),
        };
        return Current;
    }

    private PaneSnapshot RejectUnsupportedLaunch(FileSystemPath target)
    {
        Current = Current.WithActivity(
            new PaneLaunchFailed(target, FileLaunchFailureKind.ProviderUnavailable));
        return Current;
    }

    private Task<PaneSnapshot> NavigateHistoryAsync(
        PaneContentListed listed,
        PaneNavigationAction action,
        CancellationToken cancellationToken)
    {
        FileSystemPath? target = action == PaneNavigationAction.Back
            ? listed.State.NavigationHistory.BackTarget
            : listed.State.NavigationHistory.ForwardTarget;
        return target is null
            ? Task.FromResult(Current)
            : NavigateAsync(target, null, action, cancellationToken);
    }

    private async Task<PaneSnapshot> NavigateAsync(
        FileSystemPath location,
        FileSystemPath? preferredFocus,
        PaneNavigationAction action,
        CancellationToken cancellationToken)
    {
        PaneState? previousState = Current.Content is PaneContentListed previous
            ? previous.State
            : null;
        object navigation = new();
        _latestNavigation = navigation;
        Current = Current.WithActivity(new PaneLoading(location));
        DirectoryReadOutcome outcome = await ReadOwnedAsync(navigation, location, cancellationToken);
        if (!ReferenceEquals(navigation, _latestNavigation))
        {
            return Current;
        }

        Current = outcome switch
        {
            DirectoryReadSucceeded succeeded => CompleteNavigation(
                succeeded,
                previousState,
                preferredFocus,
                action),
            DirectoryReadCancelled => Current.WithActivity(new PaneReadCancelled(location)),
            DirectoryReadFailed failed => Current.WithActivity(new PaneReadFailed(location, failed.Failure)),
            _ => throw new InvalidOperationException("The directory read outcome variant is not navigable."),
        };
        return Current;
    }

    /// <summary>
    /// Reads through a token source this read owns, linked to the caller's token, so that
    /// <see cref="AbandonRead"/> can cancel exactly this read. The source is disposed when the
    /// provider returns. Only the latest navigation releases the cancel delegate, so a superseded
    /// read that returns late never detaches the delegate of the read that replaced it.
    /// </summary>
    private async Task<DirectoryReadOutcome> ReadOwnedAsync(
        object navigation,
        FileSystemPath location,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource owned = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancelLatestRead = owned.Cancel;
        try
        {
            return await _port.ReadAsync(new DirectoryReadRequest(location, _entryBoundary), owned.Token);
        }
        finally
        {
            if (ReferenceEquals(navigation, _latestNavigation))
            {
                _cancelLatestRead = CancelNothingAction;
            }
        }
    }

    /// <summary>
    /// Abandons the read in flight: a fresh navigation identity supersedes it so its late result is
    /// discarded, the abandoned state is published over the unchanged content, and only then is
    /// the read's own token cancelled, so a provider that completes synchronously on cancellation
    /// still finds the read superseded.
    /// </summary>
    private PaneSnapshot AbandonRead(PaneLoading loading)
    {
        _latestNavigation = new object();
        Current = Current.WithActivity(new PaneReadAbandoned(loading.Target));
        _cancelLatestRead();
        return Current;
    }

    private static void CancelNothing()
    {
    }

    private PaneSnapshot CompleteNavigation(
        DirectoryReadSucceeded succeeded,
        PaneState? previousState,
        FileSystemPath? preferredFocus,
        PaneNavigationAction action)
    {
        PaneState navigated = PaneReducer.Navigate(
            succeeded.Listing,
            _visiblePageCapacity,
            preferredFocus,
            ResolveHiddenItemVisibility());
        PaneState ordered = PaneReducer.ApplySortOrder(
            navigated,
            previousState?.SortOrder ?? PaneSortOrder.Default,
            preferredFocus);
        PaneState committed = PaneReducer.CommitNavigation(previousState, ordered, action);
        return PaneSnapshot.IdleWith(new PaneContentListed(committed, succeeded.Listing));
    }

    /// <summary>
    /// Names the visibility the next read applies: the listed state's own value once the pane has
    /// listed a location, and the composed starting value before that. Reading it from the state
    /// keeps one owner for the visibility (ARC-004) instead of a session field that could drift.
    /// </summary>
    private HiddenItemVisibility ResolveHiddenItemVisibility()
    {
        return Current.Content is PaneContentListed listed
            ? listed.State.HiddenItemVisibility
            : _initialHiddenItemVisibility;
    }
}
