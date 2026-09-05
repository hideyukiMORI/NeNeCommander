using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents one render-ready row: the entry it shows, the closed mark that resolves focus and
/// selection into one marker and background, and the closed rendering of the entry kind. Rows are
/// immutable and replaced only when this row's projected values change. The owned observable row
/// source notifies the host without replacing the pane's complete item source.
/// </summary>
public sealed record PaneRow
{
    internal PaneRow(DirectoryEntry entry, PaneRowMark mark, PaneRowKind kind)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(mark);
        ArgumentNullException.ThrowIfNull(kind);
        Entry = entry;
        Mark = mark;
        Kind = kind;
    }

    /// <summary>Gets the entry shown by the row.</summary>
    public DirectoryEntry Entry { get; }

    /// <summary>Gets the closed focus and selection mark.</summary>
    public PaneRowMark Mark { get; }

    /// <summary>Gets the closed rendering of the entry kind.</summary>
    public PaneRowKind Kind { get; }
}
