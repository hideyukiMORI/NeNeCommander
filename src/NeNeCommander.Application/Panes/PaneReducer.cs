using System;
using System.Collections.Generic;
using System.Linq;
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
