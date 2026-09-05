using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents an immutable snapshot of one pane: its location, every entry that location holds,
/// the hidden-item visibility that decides which of them are visible, the resulting visible set,
/// and the focus and selection addressed within that visible set. The visible set is derived, so
/// it can never disagree with the entries and the visibility that produced it.
/// </summary>
public sealed record PaneState
{
    private PaneState(
        FileSystemPath location,
        ReadOnlyCollection<DirectoryEntry> entries,
        VisiblePageCapacity visiblePageCapacity,
        HiddenItemVisibility hiddenItemVisibility)
    {
        Location = location;
        Entries = entries;
        VisiblePageCapacity = visiblePageCapacity;
        HiddenItemVisibility = hiddenItemVisibility;
        VisibleEntries = SelectVisible(entries, hiddenItemVisibility);
        FocusItem = VisibleEntries.Count == 0 ? null : VisibleEntries[0].Path;
        Selection = EmptySelection;
    }

    /// <summary>Gets the current validated location.</summary>
    public FileSystemPath Location { get; }

    /// <summary>Gets the measured number of visible rows.</summary>
    public VisiblePageCapacity VisiblePageCapacity { get; }

    /// <summary>
    /// Gets every entry of the location in listing order, including the ones the current
    /// visibility omits. Only the reducer reads it, to recover focus across a visibility change.
    /// </summary>
    internal IReadOnlyList<DirectoryEntry> Entries { get; }

    /// <summary>Gets the closed visibility that decides which entries the pane shows.</summary>
    public HiddenItemVisibility HiddenItemVisibility { get; private init; }

    /// <summary>Gets the ordered entries the pane shows under the current visibility.</summary>
    public IReadOnlyList<DirectoryEntry> VisibleEntries { get; private init; }

    /// <summary>Gets the focus item, or absence when the visible set is empty.</summary>
    public FileSystemPath? FocusItem { get; private init; }

    /// <summary>Gets the explicitly selected items, which are always visible items.</summary>
    public IReadOnlyList<FileSystemPath> Selection { get; private init; }

    private static IReadOnlyList<FileSystemPath> EmptySelection => Array.AsReadOnly(Array.Empty<FileSystemPath>());

    /// <summary>
    /// Creates an initial state after validating the entry snapshot. Focus lands on the first
    /// visible entry, or on nothing when the visibility leaves no entry visible.
    /// </summary>
    /// <param name="location">Validated pane location.</param>
    /// <param name="entries">Ordered entries of the location, hidden ones included.</param>
    /// <param name="visiblePageCapacity">Validated visible-row capacity.</param>
    /// <param name="hiddenItemVisibility">Closed visibility of hidden and system entries.</param>
    /// <returns>An accepted state or a typed rejection.</returns>
    public static PaneStateCreation Create(
        FileSystemPath location,
        IReadOnlyList<DirectoryEntry> entries,
        VisiblePageCapacity visiblePageCapacity,
        HiddenItemVisibility hiddenItemVisibility)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(visiblePageCapacity);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);

        List<DirectoryEntry> ownedEntries = [];
        HashSet<FileSystemPath> identities = new(FileSystemPathIdentityComparer.Instance);
        foreach (DirectoryEntry entry in entries)
        {
            if (entry is null)
            {
                return new PaneStateRejected(PaneStateFailureKind.NullItem);
            }
            if (!identities.Add(entry.Path))
            {
                return new PaneStateRejected(PaneStateFailureKind.DuplicateItem);
            }
            ownedEntries.Add(entry);
        }

        return new PaneStateAccepted(new PaneState(
            location,
            ownedEntries.AsReadOnly(),
            visiblePageCapacity,
            hiddenItemVisibility));
    }

    /// <summary>
    /// Creates a state from a validated listing, whose identity and boundary invariants already
    /// cover every pane invariant, so no second validation and no rejection path exist.
    /// </summary>
    internal static PaneState FromListing(
        DirectoryListing listing,
        VisiblePageCapacity visiblePageCapacity,
        HiddenItemVisibility hiddenItemVisibility)
    {
        List<DirectoryEntry> entries = [.. listing.Entries];
        return new PaneState(listing.Location, entries.AsReadOnly(), visiblePageCapacity, hiddenItemVisibility);
    }

    internal PaneState Transition(FileSystemPath? focusItem, IReadOnlyList<FileSystemPath> selection)
    {
        List<FileSystemPath> ownedSelection = [.. selection];
        return this with { FocusItem = focusItem, Selection = ownedSelection.AsReadOnly() };
    }

    /// <summary>
    /// Recomputes the visible set for another visibility, keeping focus and selection untouched.
    /// The reducer repairs both immediately afterwards, because deciding where focus lands and
    /// which selected items survive is a transition and belongs to it alone (CMD-002).
    /// </summary>
    internal PaneState WithHiddenItemVisibility(HiddenItemVisibility hiddenItemVisibility)
    {
        return this with
        {
            HiddenItemVisibility = hiddenItemVisibility,
            VisibleEntries = SelectVisible(Entries, hiddenItemVisibility),
        };
    }

    private static ReadOnlyCollection<DirectoryEntry> SelectVisible(
        IReadOnlyList<DirectoryEntry> entries,
        HiddenItemVisibility hiddenItemVisibility)
    {
        List<DirectoryEntry> visible = [.. entries.Where(entry =>
            hiddenItemVisibility == HiddenItemVisibility.Shown ||
            entry.Visibility == EntryVisibility.Normal)];
        return visible.AsReadOnly();
    }
}
