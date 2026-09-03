using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Input;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>
/// Applies every pane-local focus and selection transition through one deterministic reducer.
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
        if (offset == 0 || state.VisibleItems.Count == 0)
        {
            return state;
        }

        int currentIndex = GetFocusIndex(state);
        int targetIndex = Math.Clamp(currentIndex + offset, 0, state.VisibleItems.Count - 1);
        return state.Transition(state.VisibleItems[targetIndex], state.Selection);
    }

    /// <summary>
    /// Applies a successful location change: the listing becomes the visible snapshot, selection
    /// is cleared, and focus lands on the preferred item when it exists, otherwise on the first item.
    /// </summary>
    /// <param name="listing">Validated listing of the new location.</param>
    /// <param name="visiblePageCapacity">Validated visible-row capacity carried across locations.</param>
    /// <param name="preferredFocus">Item to focus when present, or absence to focus the first item.</param>
    /// <returns>The new valid state.</returns>
    public static PaneState Navigate(
        DirectoryListing listing,
        VisiblePageCapacity visiblePageCapacity,
        FileSystemPath? preferredFocus)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(visiblePageCapacity);

        PaneState state = PaneState.FromListing(listing, visiblePageCapacity);
        if (preferredFocus is null)
        {
            return state;
        }

        FileSystemPath? preferred = state.VisibleItems.FirstOrDefault(item =>
            FileSystemPathIdentityComparer.Instance.Equals(item, preferredFocus));
        return preferred is null ? state : state.Transition(preferred, state.Selection);
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
            return -state.VisibleItems.Count;
        }
        if (intent == UserIntent.FocusLast)
        {
            return state.VisibleItems.Count;
        }

        int halfPage = Math.Max(1, state.VisiblePageCapacity.Value / 2);
        return intent == UserIntent.MoveHalfPageDown
            ? halfPage
            : intent == UserIntent.MoveHalfPageUp ? -halfPage : 0;
    }

    private static int GetFocusIndex(PaneState state)
    {
        return state.VisibleItems
            .Index()
            .First(item => FileSystemPathIdentityComparer.Instance.Equals(item.Item, state.FocusItem))
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
