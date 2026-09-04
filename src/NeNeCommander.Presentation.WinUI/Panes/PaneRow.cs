using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents one render-ready row: the entry it shows, the closed mark that resolves focus and
/// selection into one marker and background, the closed rendering of the entry kind, and the
/// closed rendering of the entry visibility. Rows are immutable and replaced on every render, so
/// the host binds without notifications and computes nothing in the template.
/// </summary>
public sealed record PaneRow
{
    internal PaneRow(DirectoryEntry entry, PaneRowMark mark, PaneRowKind kind, PaneRowVisibility visibility)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(mark);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(visibility);
        Entry = entry;
        Mark = mark;
        Kind = kind;
        Visibility = visibility;
    }

    /// <summary>Gets the entry shown by the row.</summary>
    public DirectoryEntry Entry { get; }

    /// <summary>Gets the closed focus and selection mark.</summary>
    public PaneRowMark Mark { get; }

    /// <summary>Gets the closed rendering of the entry kind.</summary>
    public PaneRowKind Kind { get; }

    /// <summary>Gets the closed rendering of the entry visibility.</summary>
    public PaneRowVisibility Visibility { get; }
}
