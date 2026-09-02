using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents an immutable snapshot of one pane's location, visible items, focus, and selection.
/// </summary>
public sealed record PaneState
{
    private readonly ReadOnlyCollection<FileSystemPath> _selection;
    private readonly ReadOnlyCollection<FileSystemPath> _visibleItems;

    private PaneState(
        FileSystemPath location,
        ReadOnlyCollection<FileSystemPath> visibleItems,
        FileSystemPath? focusItem,
        ReadOnlyCollection<FileSystemPath> selection,
        VisiblePageCapacity visiblePageCapacity)
    {
        Location = location;
        _visibleItems = visibleItems;
        FocusItem = focusItem;
        _selection = selection;
        VisiblePageCapacity = visiblePageCapacity;
    }

    /// <summary>Gets the current validated location.</summary>
    public FileSystemPath Location { get; }

    /// <summary>Gets the ordered immutable visible item snapshot.</summary>
    public IReadOnlyList<FileSystemPath> VisibleItems => _visibleItems;

    /// <summary>Gets the focus item, or absence when the pane is empty.</summary>
    public FileSystemPath? FocusItem { get; }

    /// <summary>Gets the explicitly selected items.</summary>
    public IReadOnlyList<FileSystemPath> Selection => _selection;

    /// <summary>Gets the measured number of visible rows.</summary>
    public VisiblePageCapacity VisiblePageCapacity { get; }

    /// <summary>
    /// Creates an initial state after validating the visible snapshot.
    /// </summary>
    /// <param name="location">Validated pane location.</param>
    /// <param name="visibleItems">Ordered visible item snapshot.</param>
    /// <param name="visiblePageCapacity">Validated visible-row capacity.</param>
    /// <returns>An accepted state or a typed rejection.</returns>
    public static PaneStateCreation Create(
        FileSystemPath location,
        IReadOnlyList<FileSystemPath> visibleItems,
        VisiblePageCapacity visiblePageCapacity)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(visibleItems);
        ArgumentNullException.ThrowIfNull(visiblePageCapacity);

        List<FileSystemPath> ownedItems = [];
        HashSet<FileSystemPath> identities = new(FileSystemPathIdentityComparer.Instance);
        foreach (FileSystemPath item in visibleItems)
        {
            if (item is null)
            {
                return new PaneStateRejected(PaneStateFailureKind.NullItem);
            }
            if (!identities.Add(item))
            {
                return new PaneStateRejected(PaneStateFailureKind.DuplicateItem);
            }
            ownedItems.Add(item);
        }

        ReadOnlyCollection<FileSystemPath> visibleSnapshot = ownedItems.AsReadOnly();
        FileSystemPath? focusItem = visibleSnapshot.Count == 0 ? null : visibleSnapshot[0];
        PaneState state = new(
            location,
            visibleSnapshot,
            focusItem,
            Array.AsReadOnly(Array.Empty<FileSystemPath>()),
            visiblePageCapacity);
        return new PaneStateAccepted(state);
    }

    internal PaneState Transition(FileSystemPath? focusItem, IReadOnlyList<FileSystemPath> selection)
    {
        List<FileSystemPath> ownedSelection = [.. selection];
        return new PaneState(Location, _visibleItems, focusItem, ownedSelection.AsReadOnly(), VisiblePageCapacity);
    }
}
