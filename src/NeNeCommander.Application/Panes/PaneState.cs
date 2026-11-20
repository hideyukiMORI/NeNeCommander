using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Represents an immutable snapshot of one pane: its location, every entry that location holds,
/// the sort order that projects them, the hidden-item visibility that decides which of them are
/// visible, the resulting visible set, and the focus and selection addressed within that visible
/// set. The projected order and the visible set are derived through <see cref="EntryOrdering"/>,
/// so they can never disagree with the entries, the order, and the visibility that produced them.
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
        ListedEntries = entries;
        SortOrder = PaneSortOrder.Default;
        Entries = EntryOrdering.Apply(entries, SortOrder);
        VisiblePageCapacity = visiblePageCapacity;
        HiddenItemVisibility = hiddenItemVisibility;
        VisibleEntries = SelectVisible(Entries, hiddenItemVisibility);
        FocusItem = VisibleEntries.Count == 0 ? null : VisibleEntries[0].Path;
        Selection = EmptySelection;
        NavigationHistory = PaneNavigationHistory.Create([location], 0);
    }

    /// <summary>Gets the current validated location.</summary>
    public FileSystemPath Location { get; }

    /// <summary>Gets the measured number of visible rows.</summary>
    public VisiblePageCapacity VisiblePageCapacity { get; }

    /// <summary>
    /// Gets every entry of the location in the projected order of <see cref="SortOrder"/>,
    /// including the ones the current visibility omits. Only the reducer reads it, to recover focus
    /// across a visibility or order change.
    /// </summary>
    internal IReadOnlyList<DirectoryEntry> Entries { get; private init; }

    /// <summary>Gets the closed order that projects the entries this pane shows.</summary>
    public PaneSortOrder SortOrder { get; private init; }

    /// <summary>Gets the closed visibility that decides which entries the pane shows.</summary>
    public HiddenItemVisibility HiddenItemVisibility { get; private init; }

    /// <summary>
    /// Gets the entries the pane shows under the current visibility, in the projected order of
    /// <see cref="SortOrder"/>. Movement, paging, focus, selection, and row projection read this
    /// sequence.
    /// </summary>
    public IReadOnlyList<DirectoryEntry> VisibleEntries { get; private init; }

    /// <summary>
    /// Gets the exact path instance of one visible entry, or absence when the visible set is empty.
    /// Every completed reducer transition preserves this relationship.
    /// </summary>
    public FileSystemPath? FocusItem { get; private init; }

    /// <summary>
    /// Gets the owned immutable selection of visible items. Movement and sorting retain this
    /// collection; selection changes create a separately owned snapshot.
    /// </summary>
    public IReadOnlyList<FileSystemPath> Selection { get; private init; }

    /// <summary>
    /// Gets this pane's bounded successful-location sequence. Only the reducer replaces it.
    /// </summary>
    internal PaneNavigationHistory NavigationHistory { get; private init; }

    /// <summary>Gets every entry in the order it was given, which the projection never changes.</summary>
    private IReadOnlyList<DirectoryEntry> ListedEntries { get; }

    private static IReadOnlyList<FileSystemPath> EmptySelection => SelectionSnapshot.Create(Array.Empty<FileSystemPath>());

    /// <summary>
    /// Creates an initial state after validating the entry snapshot. The state starts with
    /// <see cref="PaneSortOrder.Default"/>. Focus lands on the first visible entry, or on nothing
    /// when the visibility leaves no entry visible.
    /// </summary>
    /// <param name="location">Validated pane location.</param>
    /// <param name="entries">Entries of the location in any order, hidden ones included.</param>
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

    /// <summary>
    /// Commits the reducer's repaired focus and selection. The focus must be an actual visible
    /// entry's path instance, or null when the visible set is empty. External preferred paths
    /// must already have been resolved by identity. Only this state's owned selection is reused;
    /// every other collection is copied so mutable caller storage cannot change either state.
    /// </summary>
    internal PaneState Transition(FileSystemPath? focusItem, IReadOnlyList<FileSystemPath> selection)
    {
        return this with
        {
            FocusItem = focusItem,
            Selection = ReferenceEquals(selection, Selection) ? Selection : SelectionSnapshot.Create(selection),
        };
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

    /// <summary>
    /// Recomputes the projected order and the visible set for another sort order, keeping focus
    /// and selection untouched. The reducer repairs focus immediately afterwards (CMD-002).
    /// </summary>
    internal PaneState WithSortOrder(PaneSortOrder sortOrder)
    {
        IReadOnlyList<DirectoryEntry> ordered = EntryOrdering.Apply(ListedEntries, sortOrder);
        return this with
        {
            SortOrder = sortOrder,
            Entries = ordered,
            VisibleEntries = SelectVisible(ordered, HiddenItemVisibility),
        };
    }

    /// <summary>
    /// Attaches the history snapshot produced by the reducer after verifying that its cursor names
    /// this listed location.
    /// </summary>
    internal PaneState WithNavigationHistory(PaneNavigationHistory navigationHistory)
    {
        ArgumentNullException.ThrowIfNull(navigationHistory);
        FileSystemPath current = navigationHistory.Locations[navigationHistory.CurrentIndex];
        return FileSystemPathIdentityComparer.Instance.Equals(Location, current)
            ? this with { NavigationHistory = navigationHistory }
            : throw new ArgumentException(
                "History current must identify the pane location.",
                nameof(navigationHistory));
    }

    private static ReadOnlyCollection<DirectoryEntry> SelectVisible(
        IReadOnlyList<DirectoryEntry> entries,
        HiddenItemVisibility hiddenItemVisibility)
    {
        List<DirectoryEntry> visible = [.. entries.Where(entry =>
            hiddenItemVisibility == HiddenItemVisibility.Shown ||
            entry.Metadata.Visibility == EntryVisibility.Normal)];
        return visible.AsReadOnly();
    }

    /// <summary>
    /// Closes the selection's backing storage while retaining generic collection read/copy paths.
    /// Non-generic ICollection is deliberately absent because its SyncRoot can expose List storage.
    /// </summary>
    private sealed record SelectionSnapshot : IReadOnlyList<FileSystemPath>, ICollection<FileSystemPath>
    {
        private readonly List<FileSystemPath> _paths;

        private SelectionSnapshot(IReadOnlyList<FileSystemPath> paths)
        {
            _paths = [.. paths];
        }

        /// <summary>Gets the number of selected paths without enumeration.</summary>
        public int Count => _paths.Count;

        /// <summary>Gets one selected path with the existing List index-boundary behavior.</summary>
        public FileSystemPath this[int index] => _paths[index];

        bool ICollection<FileSystemPath>.IsReadOnly => true;

        internal static SelectionSnapshot Create(IReadOnlyList<FileSystemPath> paths)
        {
            return new SelectionSnapshot(paths);
        }

        /// <summary>Enumerates the owned paths without exposing their collection.</summary>
        public IEnumerator<FileSystemPath> GetEnumerator()
        {
            return ((IEnumerable<FileSystemPath>)_paths).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        bool ICollection<FileSystemPath>.Contains(FileSystemPath path)
        {
            return _paths.Contains(path);
        }

        void ICollection<FileSystemPath>.CopyTo(FileSystemPath[] array, int arrayIndex)
        {
            _paths.CopyTo(array, arrayIndex);
        }

        void ICollection<FileSystemPath>.Add(FileSystemPath path)
        {
            throw new NotSupportedException();
        }

        void ICollection<FileSystemPath>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<FileSystemPath>.Remove(FileSystemPath path)
        {
            throw new NotSupportedException();
        }
    }
}
