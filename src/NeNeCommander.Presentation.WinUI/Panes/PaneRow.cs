using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents one render-ready row: the entry it shows, the closed mark that resolves focus and
/// selection into one marker and background, the closed rendering of the entry kind, the closed
/// rendering of the entry visibility, and the size and modification texts. Rows are immutable and
/// replaced only when this row's projected values change. The owned observable row source
/// notifies the host without replacing the pane's complete item source.
/// </summary>
public sealed record PaneRow
{
    internal PaneRow(DirectoryEntry entry, PaneRowMark mark, EntryMetadataFormat format)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(mark);
        ArgumentNullException.ThrowIfNull(format);
        Entry = entry;
        Mark = mark;
        Kind = PaneRowKind.For(entry.Kind);
        Visibility = PaneRowVisibility.For(entry.Metadata.Visibility);
        SizeText = Kind.IsDirectory
            ? string.Empty
            : EntryMetadataFormatter.FormatSize(entry.Metadata.Size, format);
        ModifiedText = EntryMetadataFormatter.FormatModified(entry.Metadata.Modified, format);
    }

    private PaneRow(PaneRow source, PaneRowMark mark)
    {
        Entry = source.Entry;
        Mark = mark;
        Kind = source.Kind;
        Visibility = source.Visibility;
        SizeText = source.SizeText;
        ModifiedText = source.ModifiedText;
    }

    /// <summary>Gets the entry shown by the row.</summary>
    public DirectoryEntry Entry { get; }

    /// <summary>Gets the closed focus and selection mark.</summary>
    public PaneRowMark Mark { get; }

    /// <summary>Gets the closed rendering of the entry kind.</summary>
    public PaneRowKind Kind { get; }

    /// <summary>Gets the closed rendering of the entry visibility.</summary>
    public PaneRowVisibility Visibility { get; }

    /// <summary>
    /// Gets the size text: empty for a directory, whose kind label already says what it is,
    /// otherwise the formatted size or the unknown glyph.
    /// </summary>
    public string SizeText { get; }

    /// <summary>Gets the local modification time text or the unknown glyph.</summary>
    public string ModifiedText { get; }

    /// <summary>
    /// Creates the same row with another mark. The texts are kept because a row is reused only
    /// while the projection receives the same listing and the same metadata format, so its
    /// metadata texts cannot have changed.
    /// </summary>
    /// <param name="mark">Closed mark the row now carries.</param>
    /// <returns>A row that differs from this one only in its mark.</returns>
    internal PaneRow WithMark(PaneRowMark mark)
    {
        ArgumentNullException.ThrowIfNull(mark);
        return new PaneRow(this, mark);
    }
}
