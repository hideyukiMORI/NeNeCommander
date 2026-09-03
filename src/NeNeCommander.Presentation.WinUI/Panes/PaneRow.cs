using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents one render-ready row: the entry it shows and whether it is explicitly selected.
/// Rows are immutable and replaced on every render, so the host binds without notifications.
/// </summary>
public sealed record PaneRow
{
    internal PaneRow(DirectoryEntry entry, PaneRowMark mark)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(mark);
        Entry = entry;
        Mark = mark;
    }

    /// <summary>Gets the entry shown by the row.</summary>
    public DirectoryEntry Entry { get; }

    /// <summary>Gets the closed selection mark.</summary>
    public PaneRowMark Mark { get; }

    /// <summary>Gets whether the row is inside the explicit selection, for framework visibility binding.</summary>
    public bool IsSelected => Mark == PaneRowMark.Selected;
}
