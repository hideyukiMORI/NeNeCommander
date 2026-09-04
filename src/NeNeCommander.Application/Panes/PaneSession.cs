using System;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Coordinates one pane's navigation: it owns the current <see cref="PaneSnapshot"/>, routes
/// focus and selection intents through <see cref="PaneReducer"/>, and performs location changes
/// through the sole directory read port. It is not thread-safe and is driven from one owner.
/// </summary>
public sealed class PaneSession
{
    private readonly int _entryBoundary;
    private readonly IDirectoryReadPort _port;
    private readonly VisiblePageCapacity _visiblePageCapacity;
    private object? _latestNavigation;

    /// <summary>Initializes an empty session over one read port.</summary>
    /// <param name="port">Provider-neutral directory read port.</param>
    /// <param name="visiblePageCapacity">Validated visible-row capacity used for paging.</param>
    /// <param name="entryBoundary">Entry boundary applied to every read, within the fixed range.</param>
    /// <exception cref="ArgumentOutOfRangeException">The boundary is outside the fixed range, which is a composition defect.</exception>
    public PaneSession(IDirectoryReadPort port, VisiblePageCapacity visiblePageCapacity, int entryBoundary)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(visiblePageCapacity);
        if (!DirectoryReadRequest.IsValidEntryBoundary(entryBoundary))
        {
            throw new ArgumentOutOfRangeException(nameof(entryBoundary));
        }
        _port = port;
        _visiblePageCapacity = visiblePageCapacity;
        _entryBoundary = entryBoundary;
        Current = PaneSnapshot.Initial;
    }

    /// <summary>Gets the current immutable snapshot.</summary>
    public PaneSnapshot Current { get; private set; }

    /// <summary>
    /// Reads a location and, on success, replaces the content with focus on the first entry.
    /// A newer navigation supersedes this one: a superseded result is discarded.
    /// </summary>
    /// <param name="location">Validated location to read.</param>
    /// <param name="cancellationToken">Token observed by the read.</param>
    /// <returns>The snapshot current after the read completed or was superseded.</returns>
    public Task<PaneSnapshot> NavigateAsync(FileSystemPath location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);
        return NavigateAsync(location, null, cancellationToken);
    }

    /// <summary>
    /// Re-reads the listed location, keeping the current focus item when it still exists and
    /// clearing selection. Nothing happens before the first listing or while a read is in flight.
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
    /// clearing selection. Nothing happens before the first listing or while a read is in flight.
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
        return Current.Activity is PaneLoading
            ? Task.FromResult(Current)
            : NavigateAsync(listed.State.Location, preferredFocus, cancellationToken);
    }

    /// <summary>
    /// Applies one intent. Movement and selection use the reducer; opening a directory entry and
    /// navigating to the parent start a read; refresh re-reads the current location. Intents are
    /// frozen while a read is in flight.
    /// </summary>
    /// <param name="intent">Typed user intent.</param>
    /// <param name="cancellationToken">Token observed by any read the intent starts.</param>
    /// <returns>The resulting snapshot.</returns>
    public Task<PaneSnapshot> HandleAsync(UserIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (Current.Activity is PaneLoading || Current.Content is not PaneContentListed listed)
        {
            return Task.FromResult(Current);
        }
        if (intent == UserIntent.OpenFocused)
        {
            return OpenFocusedAsync(listed, cancellationToken);
        }
        if (intent == UserIntent.Refresh)
        {
            return NavigateAsync(listed.State.Location, listed.State.FocusItem, cancellationToken);
        }
        if (intent == UserIntent.NavigateParent)
        {
            FileSystemPath? parent = listed.State.Location.Parent;
            return parent is null
                ? Task.FromResult(Current)
                : NavigateAsync(parent, listed.State.Location, cancellationToken);
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
        return focused is not null && focused.Kind == DirectoryEntryKind.Directory
            ? NavigateAsync(focused.Path, null, cancellationToken)
            : Task.FromResult(Current);
    }

    private async Task<PaneSnapshot> NavigateAsync(
        FileSystemPath location,
        FileSystemPath? preferredFocus,
        CancellationToken cancellationToken)
    {
        object navigation = new();
        _latestNavigation = navigation;
        Current = Current.WithActivity(new PaneLoading(location));
        DirectoryReadOutcome outcome = await _port.ReadAsync(
            new DirectoryReadRequest(location, _entryBoundary),
            cancellationToken);
        if (!ReferenceEquals(navigation, _latestNavigation))
        {
            return Current;
        }

        Current = outcome switch
        {
            DirectoryReadSucceeded succeeded => PaneSnapshot.IdleWith(new PaneContentListed(
                PaneReducer.Navigate(succeeded.Listing, _visiblePageCapacity, preferredFocus),
                succeeded.Listing)),
            DirectoryReadCancelled => Current.WithActivity(new PaneReadCancelled(location)),
            DirectoryReadFailed failed => Current.WithActivity(new PaneReadFailed(location, failed.Failure)),
            _ => throw new InvalidOperationException("The directory read outcome variant is not navigable."),
        };
        return Current;
    }
}
