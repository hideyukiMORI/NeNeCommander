using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Applies every pane-local focus, selection, and hidden-item visibility transition through one
/// deterministic reducer. It is the sole decider of which entries a pane shows: movement, paging,
/// first and last, and selection all address the visible set alone (CMD-002).
/// </summary>
public static class PaneReducer
{
    /// <summary>
    /// Applies one user intent to an immutable pane state.
    /// </summary>
    /// <param name="state">Current pane state.</param>
    /// <param name="intent">Typed user intent.</param>
    /// <returns>The original state for irrelevant intents, otherwise a new valid state.</returns>
    public static PaneState Apply(PaneState state, UserIntent intent)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(intent);

        if (intent == UserIntent.ToggleSelection)
        {
            return ToggleSelection(state);
        }
        if (intent == UserIntent.Escape)
        {
            return state.Transition(state.FocusItem, Array.Empty<FileSystemPath>());
        }

        int offset = GetMovementOffset(state, intent);
        if (offset == 0 || state.VisibleEntries.Count == 0)
        {
            return state;
        }

        int currentIndex = GetFocusIndex(state);
        int targetIndex = Math.Clamp(currentIndex + offset, 0, state.VisibleEntries.Count - 1);
        return state.Transition(state.VisibleEntries[targetIndex].Path, state.Selection);
    }

    /// <summary>
    /// Applies a successful location change: the listing becomes the entry snapshot, the given
    /// visibility decides the visible set, selection is cleared, and focus lands on the preferred
    /// item when it is visible, on the nearest visible entry when the preferred item is hidden,
    /// and on the first visible entry when the listing does not hold the preferred item at all.
    /// </summary>
    /// <param name="listing">Validated listing of the new location.</param>
    /// <param name="visiblePageCapacity">Validated visible-row capacity carried across locations.</param>
    /// <param name="preferredFocus">Item to focus when present, or absence to focus the first visible entry.</param>
    /// <param name="hiddenItemVisibility">Closed visibility carried into the new location.</param>
    /// <returns>The new valid state.</returns>
    public static PaneState Navigate(
        DirectoryListing listing,
        VisiblePageCapacity visiblePageCapacity,
        FileSystemPath? preferredFocus,
        HiddenItemVisibility hiddenItemVisibility)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(visiblePageCapacity);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);

        PaneState state = PaneState.FromListing(listing, visiblePageCapacity, hiddenItemVisibility);
        FileSystemPath? recovered = RecoverFocus(state, preferredFocus);
        return recovered is null ? state : state.Transition(recovered, state.Selection);
    }

    /// <summary>
    /// Applies another hidden-item visibility to the same location. The focus item is kept when it
    /// stays visible and otherwise moves to the nearest visible entry, and every selected item that
    /// is no longer visible leaves the selection.
    /// </summary>
    /// <param name="state">Current pane state.</param>
    /// <param name="hiddenItemVisibility">Closed visibility to apply.</param>
    /// <returns>The state whose visible set, focus, and selection obey the new visibility.</returns>
    public static PaneState ApplyHiddenItemVisibility(PaneState state, HiddenItemVisibility hiddenItemVisibility)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(hiddenItemVisibility);

        PaneState rebuilt = state.WithHiddenItemVisibility(hiddenItemVisibility);
        return rebuilt.Transition(
            RecoverFocusOrFirst(rebuilt, state.FocusItem),
            RetainVisibleSelection(rebuilt, state.Selection));
    }

    private static FileSystemPath? RecoverFocusOrFirst(PaneState state, FileSystemPath? target)
    {
        FileSystemPath? recovered = RecoverFocus(state, target);
        return recovered ?? (state.VisibleEntries.Count == 0 ? null : state.VisibleEntries[0].Path);
    }

    /// <summary>
    /// Names the entry a pane focuses for a target that may be hidden or absent: the target itself
    /// when it is visible, otherwise the next visible entry in listing order, otherwise the
    /// previous visible entry, otherwise absence. A target the location does not hold at all is
    /// absence too, which leaves the caller's own default focus in place.
    /// </summary>
    private static FileSystemPath? RecoverFocus(PaneState state, FileSystemPath? target)
    {
        int index = IndexOfEntry(state.Entries, target);
        return index < 0 ? null : NearestVisible(state, index);
    }

    private static FileSystemPath? NearestVisible(PaneState state, int index)
    {
        HashSet<FileSystemPath> visible = VisibleIdentities(state);
        for (int forward = index; forward < state.Entries.Count; forward++)
        {
            if (visible.Contains(state.Entries[forward].Path))
            {
                return state.Entries[forward].Path;
            }
        }
        for (int backward = index - 1; backward >= 0; backward--)
        {
            if (visible.Contains(state.Entries[backward].Path))
            {
                return state.Entries[backward].Path;
            }
        }
        return null;
    }

    private static List<FileSystemPath> RetainVisibleSelection(
        PaneState state,
        IReadOnlyList<FileSystemPath> selection)
    {
        HashSet<FileSystemPath> visible = VisibleIdentities(state);
        List<FileSystemPath> retained = [];
        foreach (FileSystemPath item in selection)
        {
            if (visible.Contains(item))
            {
                retained.Add(item);
            }
        }
        return retained;
    }

    private static HashSet<FileSystemPath> VisibleIdentities(PaneState state)
    {
        return new HashSet<FileSystemPath>(
            state.VisibleEntries.Select(entry => entry.Path),
            FileSystemPathIdentityComparer.Instance);
    }

    private static int IndexOfEntry(IReadOnlyList<DirectoryEntry> entries, FileSystemPath? target)
    {
        for (int index = 0; index < entries.Count; index++)
        {
            if (FileSystemPathIdentityComparer.Instance.Equals(entries[index].Path, target))
            {
                return index;
            }
        }
        return -1;
    }

    private static int GetMovementOffset(PaneState state, UserIntent intent)
    {
        if (intent == UserIntent.MoveNext)
        {
            return 1;
        }
        if (intent == UserIntent.MovePrevious)
        {
            return -1;
        }
        if (intent == UserIntent.FocusFirst)
        {
            return -state.VisibleEntries.Count;
        }
        if (intent == UserIntent.FocusLast)
        {
            return state.VisibleEntries.Count;
        }

        int halfPage = Math.Max(1, state.VisiblePageCapacity.Value / 2);
        return intent == UserIntent.MoveHalfPageDown
            ? halfPage
            : intent == UserIntent.MoveHalfPageUp ? -halfPage : 0;
    }

    private static int GetFocusIndex(PaneState state)
    {
        return state.VisibleEntries
            .Index()
            .First(item => FileSystemPathIdentityComparer.Instance.Equals(item.Item.Path, state.FocusItem))
            .Index;
    }

    private static PaneState ToggleSelection(PaneState state)
    {
        if (state.FocusItem is null)
        {
            return state;
        }

        List<FileSystemPath> selection = [.. state.Selection];
        int selectedIndex = selection.FindIndex(item =>
            FileSystemPathIdentityComparer.Instance.Equals(item, state.FocusItem));
        if (selectedIndex < 0)
        {
            selection.Add(state.FocusItem);
        }
        else
        {
            selection.RemoveAt(selectedIndex);
        }
        return state.Transition(state.FocusItem, selection);
    }
}
