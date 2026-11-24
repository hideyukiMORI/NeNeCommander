using System;
using System.Runtime.ExceptionServices;
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
/// sole directory read port, and hands focused files to the sole launcher boundary. Each read is
/// one <see cref="PaneRead"/> with its own token source; <see cref="UserIntent.Escape"/> abandons
/// the read in flight, completes its waiting caller at once, and leaves the provider task to run
/// to completion with its result discarded (ADR-0058). A fault of such an orphaned read is rethrown
/// at the next entry point. It is not thread-safe and is driven from one owner.
/// </summary>
public sealed class PaneSession
{
    private static readonly Action<PaneSnapshot> AbandonNothingAction = AbandonNothing;

    private readonly int _entryBoundary;
    private readonly HiddenItemVisibility _initialHiddenItemVisibility;
    private readonly PaneSortOrder _initialSortOrder;
    private readonly IFileLauncher _fileLauncher;
    private readonly IDirectoryReadPort _port;
    private readonly VisiblePageCapacity _visiblePageCapacity;
    private object? _latestNavigation;
    private Action<PaneSnapshot> _abandonLatestRead = AbandonNothingAction;
    private ExceptionDispatchInfo? _orphanedReadFault;

    /// <summary>Initializes an empty session over one read port.</summary>
    /// <param name="port">Provider-neutral directory read port.</param>
    /// <param name="fileLauncher">Sole provider boundary for a focused file handoff.</param>
    /// <param name="configuration">Validated limits and preferences used before the first listing.</param>
    public PaneSession(
        IDirectoryReadPort port,
        IFileLauncher fileLauncher,
        PaneSessionConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(fileLauncher);
        ArgumentNullException.ThrowIfNull(configuration);
        _port = port;
        _fileLauncher = fileLauncher;
        _visiblePageCapacity = configuration.VisiblePageCapacity;
        _entryBoundary = configuration.EntryBoundary;
        _initialHiddenItemVisibility = configuration.HiddenItemVisibility;
        _initialSortOrder = configuration.SortOrder;
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
        ThrowOrphanedReadFault();
        return Current.Activity is PaneLaunching
            ? Task.FromResult(Current)
            : NavigateAsync(location, null, PaneNavigationAction.Append, cancellationToken);
    }

    /// <summary>
    /// Re-reads the listed location, keeping the current focus item when it still exists and
    /// clearing selection. A removed focus recovers the next surviving item in the old visible
    /// order, then the previous, then the first new visible item if no old item survives. Nothing
    /// happens before the first listing or while a read or file handoff is in flight.
    /// </summary>
    /// <param name="cancellationToken">Token observed by the read.</param>
    /// <returns>The snapshot current after the read completed or was superseded.</returns>
    public Task<PaneSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        ThrowOrphanedReadFault();
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
        ThrowOrphanedReadFault();
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
        ThrowOrphanedReadFault();
        if (intent is PaneInteraction interaction)
        {
            return HandleInteractionAsync(interaction, cancellationToken);
        }
        if (Current.Activity is PaneLoading loading)
        {
            return Task.FromResult(intent == UserIntent.Escape ? AbandonRead(loading) : Current);
        }
        if (Current.Activity is PaneLaunching ||
            Current.Content is not PaneContentListed listed)
        {
            return Task.FromResult(Current);
        }
        Task<PaneSnapshot>? external = StartExternalAction(listed, intent, cancellationToken);
        if (external is not null)
        {
            return external;
        }

        PaneState next = PaneReducer.Apply(listed.State, intent);
        if (!ReferenceEquals(next, listed.State))
        {
            Current = PaneSnapshot.IdleWith(new PaneContentListed(next, listed.Listing, listed.DriveKind));
        }
        return Task.FromResult(Current);
    }

    /// <summary>
    /// Applies admitted pointer input and retains an immutable receipt even for passive surfaces.
    /// </summary>
    private Task<PaneSnapshot> HandleInteractionAsync(PaneInteraction interaction, CancellationToken cancellationToken)
    {
        PaneSnapshot before = Current;
        _ = ApplyInteraction(interaction);
        return !ReferenceEquals(before, Current) &&
            interaction is PaneRowInteraction row && row.Action == PaneEntryAction.Open
                ? OpenFocusedAsync((PaneContentListed)Current.Content, cancellationToken)
                : Task.FromResult(Current);
    }

    /// <summary>Applies the sole synchronous qualified selection transition and returns its receipt.</summary>
    internal PaneSnapshot ApplyInteraction(PaneInteraction interaction)
    {
        ThrowOrphanedReadFault();
        if (!interaction.Matches(Current))
        {
            return Current;
        }
        PaneContent content = Current.Content;
        if (content is PaneContentListed listed)
        {
            PaneState next = PaneReducer.ApplyInteraction(listed.State, interaction);
            if (!ReferenceEquals(next, listed.State))
            {
                content = new PaneContentListed(next, listed.Listing, listed.DriveKind);
            }
        }
        Current = Current.WithContent(content);
        return Current;
    }

    /// <summary>Starts the existing external action requested by an ordinary pane intent.</summary>
    private Task<PaneSnapshot>? StartExternalAction(
        PaneContentListed listed,
        UserIntent intent,
        CancellationToken cancellationToken)
    {
        return intent switch
        {
            _ when intent == UserIntent.OpenFocused => OpenFocusedAsync(listed, cancellationToken),
            _ when intent == UserIntent.RevealLocation => HandoffAsync(listed.State.Location, FileLaunchPurpose.Reveal, cancellationToken),
            _ when intent == UserIntent.RevealFocused && listed.State.FocusItem is FileSystemPath focus =>
                HandoffAsync(focus, FileLaunchPurpose.Reveal, cancellationToken),
            _ => StartNavigationAction(listed, intent, cancellationToken),
        };
    }

    private Task<PaneSnapshot>? StartNavigationAction(
        PaneContentListed listed,
        UserIntent intent,
        CancellationToken cancellationToken)
    {
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
        return intent == UserIntent.NavigateBack
            ? NavigateHistoryAsync(listed, PaneNavigationAction.Back, cancellationToken)
            : intent == UserIntent.NavigateForward
                ? NavigateHistoryAsync(listed, PaneNavigationAction.Forward, cancellationToken)
                : null;
    }

    private Task<PaneSnapshot> OpenFocusedAsync(PaneContentListed listed, CancellationToken cancellationToken)
    {
        DirectoryEntry? focused = listed.FindFocusedEntry();
        return focused is null
            ? Task.FromResult(Current)
            : focused.Kind == DirectoryEntryKind.Directory
                ? NavigateAsync(focused.Path, null, PaneNavigationAction.Append, cancellationToken)
                : HandoffAsync(focused.Path, FileLaunchPurpose.Activation, cancellationToken);
    }

    private async Task<PaneSnapshot> HandoffAsync(
        FileSystemPath target,
        FileLaunchPurpose purpose,
        CancellationToken cancellationToken)
    {
        PaneContent content = Current.Content;
        Current = Current.WithActivity(new PaneLaunching(target, purpose));
        FileLaunchOutcome outcome = purpose == FileLaunchPurpose.Activation
            ? await _fileLauncher.LaunchAsync(target, cancellationToken)
            : await _fileLauncher.RevealAsync(target, cancellationToken);
        Current = outcome switch
        {
            FileLaunchAccepted => PaneSnapshot.IdleWith(content),
            FileLaunchCancelled => Current.WithActivity(new PaneLaunchCancelled(target, purpose)),
            FileLaunchFailed failed => Current.WithActivity(new PaneLaunchFailed(target, purpose, failed.Failure)),
            _ => throw new InvalidOperationException("The file launch outcome variant is not supported."),
        };
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
        PaneRead read = PaneRead.Start(
            _port,
            new DirectoryReadRequest(location, _entryBoundary),
            RecordOrphanedReadFault,
            cancellationToken);
        _latestNavigation = read;
        _abandonLatestRead = read.Abandon;
        Current = Current.WithActivity(new PaneLoading(location));
        _ = await Task.WhenAny(read.Provider, read.Abandonment);
        if (read.IsAbandoned)
        {
            return await read.Abandonment;
        }
        DirectoryReadOutcome outcome = await read.Provider;
        if (!ReferenceEquals(read, _latestNavigation))
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
    /// Abandons the read in flight: a fresh navigation identity supersedes it, the abandoned state
    /// is published over the unchanged content, and only then is the read completed for its
    /// waiting caller and its own token cancelled.
    /// </summary>
    private PaneSnapshot AbandonRead(PaneLoading loading)
    {
        _latestNavigation = new object();
        Current = Current.WithActivity(new PaneReadAbandoned(loading.Target));
        _abandonLatestRead(Current);
        return Current;
    }

    /// <summary>
    /// Keeps the fault of an abandoned read whose caller no longer waits, so the next entry
    /// point rethrows it on the owner's existing defect path instead of losing it.
    /// </summary>
    private void RecordOrphanedReadFault(ExceptionDispatchInfo fault)
    {
        _orphanedReadFault = fault;
    }

    private void ThrowOrphanedReadFault()
    {
        ExceptionDispatchInfo? fault = _orphanedReadFault;
        _orphanedReadFault = null;
        fault?.Throw();
    }

    private static void AbandonNothing(PaneSnapshot abandoned)
    {
        _ = abandoned;
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
            previousState?.SortOrder ?? _initialSortOrder,
            preferredFocus);
        if (action == PaneNavigationAction.Preserve && previousState is not null)
        {
            ordered = PaneReducer.RecoverRefreshFocus(previousState, ordered, preferredFocus);
        }
        PaneState committed = PaneReducer.CommitNavigation(previousState, ordered, action);
        return PaneSnapshot.IdleWith(new PaneContentListed(committed, succeeded.Listing, succeeded.DriveKind));
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
