using System.Collections.Generic;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the closed, render-ready projection of one directory read for one pane.
/// The host assigns these values to framework controls without making further decisions.
/// </summary>
public abstract record PanePresentation
{
    private protected PanePresentation()
    {
    }

    /// <summary>Gets the status the pane shows for this projection.</summary>
    public abstract PaneStatus Status { get; }

    /// <summary>Gets the ordered rows to display, or an empty list when no listing exists.</summary>
    public abstract IReadOnlyList<DirectoryEntry> Entries { get; }

    /// <summary>Gets the entry that receives initial focus, or absence when there are no rows.</summary>
    public abstract DirectoryEntry? FocusEntry { get; }
}
