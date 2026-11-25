using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Panes;

/// <summary>Calculates selection values for the sole pane reducer without owning or committing state.</summary>
internal static class PaneSelection
{
    internal static IReadOnlyList<FileSystemPath> Toggle(IReadOnlyList<FileSystemPath> current, FileSystemPath target)
    {
        List<FileSystemPath> selection = [.. current];
        int selectedIndex = selection.FindIndex(item => FileSystemPathIdentityComparer.Instance.Equals(item, target));
        if (selectedIndex < 0)
        {
            selection.Add(target);
        }
        else
        {
            selection.RemoveAt(selectedIndex);
        }
        return selection;
    }

    internal static IReadOnlyList<FileSystemPath> Range(
        PaneState state,
        FileSystemPath anchor,
        FileSystemPath target,
        PaneEntryAction action)
    {
        int anchorIndex = IndexOfOwnedPath(state, anchor);
        int targetIndex = IndexOfOwnedPath(state, target);
        List<FileSystemPath> selection = action == PaneEntryAction.AddRange ? [.. state.Selection] : [];
        HashSet<FileSystemPath> selected = new(selection, FileSystemPathIdentityComparer.Instance);
        for (int index = Math.Min(anchorIndex, targetIndex); index <= Math.Max(anchorIndex, targetIndex); index++)
        {
            FileSystemPath path = state.VisibleEntries[index].Path;
            if (selected.Add(path))
            {
                selection.Add(path);
            }
        }
        return selection;
    }

    internal static IReadOnlyList<FileSystemPath> All(PaneState state)
    {
        return [.. state.VisibleEntries.Select(entry => entry.Path)];
    }

    internal static IReadOnlyList<FileSystemPath> Invert(PaneState state)
    {
        HashSet<FileSystemPath> selected = new(state.Selection, FileSystemPathIdentityComparer.Instance);
        return [.. state.VisibleEntries.Select(entry => entry.Path).Where(path => !selected.Contains(path))];
    }

    internal static FileSystemPath? RebindAnchor(PaneState state, FileSystemPath? anchor)
    {
        return state.VisibleEntries.FirstOrDefault(entry =>
            FileSystemPathIdentityComparer.Instance.Equals(entry.Path, anchor))?.Path;
    }

    private static int IndexOfOwnedPath(PaneState state, FileSystemPath path)
    {
        for (int index = 0; index < state.VisibleEntries.Count; index++)
        {
            if (ReferenceEquals(state.VisibleEntries[index].Path, path))
            {
                return index;
            }
        }
        throw new InvalidOperationException("Range endpoints must reference visible entry paths.");
    }
}
