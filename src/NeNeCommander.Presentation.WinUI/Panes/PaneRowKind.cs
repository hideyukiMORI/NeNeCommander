using System;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Identifies the closed rendering of one entry kind: which of the two vector shapes the row draws
/// and the localization resource that labels it beside the name. The mapping from the application's
/// <see cref="DirectoryEntryKind"/> happens once here so no view decides what a kind means.
/// </summary>
public abstract record PaneRowKind
{
    /// <summary>Gets the rendering of an entry that can itself be read as a directory.</summary>
    public static PaneRowKind Directory { get; } = new DirectoryRowKind();

    /// <summary>Gets the rendering of an entry that cannot be read as a directory.</summary>
    public static PaneRowKind File { get; } = new FileRowKind();

    private PaneRowKind()
    {
    }

    /// <summary>Gets the localization resource key of the kind label; the file kind names an empty label.</summary>
    public abstract string LabelResourceKey { get; }

    /// <summary>
    /// Gets whether the row draws the directory shape. The framework cannot bind one element to a
    /// geometry chosen at run time, so the row template holds both shapes and shows this one.
    /// </summary>
    public abstract bool IsDirectory { get; }

    /// <summary>
    /// Gets whether the row draws the file shape. Exactly one of the two shape properties is true
    /// for any kind because both are derived from this closed hierarchy and neither can be set.
    /// </summary>
    public abstract bool IsFile { get; }

    /// <summary>Translates the application's closed entry kind into its rendering.</summary>
    /// <param name="kind">Closed entry kind reported by the provider.</param>
    /// <returns>The rendering of that kind.</returns>
    public static PaneRowKind For(DirectoryEntryKind kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return kind == DirectoryEntryKind.Directory ? Directory : File;
    }

    private sealed record DirectoryRowKind : PaneRowKind
    {
        public override string LabelResourceKey => "EntryKindDirectoryLabel";

        public override bool IsDirectory => true;

        public override bool IsFile => false;
    }

    private sealed record FileRowKind : PaneRowKind
    {
        public override string LabelResourceKey => "EntryKindFileLabel";

        public override bool IsDirectory => false;

        public override bool IsFile => true;
    }
}
