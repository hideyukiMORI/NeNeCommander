using System;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.Panes;

namespace NeNeCommander.Application.Input;

/// <summary>Requests one closed row action for an entry owned by the rendered pane.</summary>
public sealed record PaneRowInteraction : PaneInteraction
{
    private protected override void RestrictDerivation()
    {
    }

    internal PaneRowInteraction(PaneSide side, PaneSnapshot expectedSnapshot, DirectoryEntry entry, PaneEntryAction action)
        : base(side, expectedSnapshot)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(action);
        Entry = entry;
        Action = action;
    }

    /// <summary>Gets the exact entry shown by the row, never an index or path-text lookup.</summary>
    public DirectoryEntry Entry { get; }

    /// <summary>Gets the requested row selection or opening action.</summary>
    public PaneEntryAction Action { get; }
}
