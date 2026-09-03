using System;
using System.Collections.Generic;
using NeNeCommander.Application.Directories;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the render-ready projection of one pane snapshot. The host assigns these values
/// to framework controls without making further decisions.
/// </summary>
public sealed record PanePresentation
{
    internal PanePresentation(
        IReadOnlyList<DirectoryEntry> entries,
        DirectoryEntry? focusEntry,
        PaneStatus status,
        string addressText)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(addressText);
        Entries = entries;
        FocusEntry = focusEntry;
        Status = status;
        AddressText = addressText;
    }

    /// <summary>Gets the ordered rows to display, or an empty list when nothing is listed.</summary>
    public IReadOnlyList<DirectoryEntry> Entries { get; }

    /// <summary>Gets the row that holds focus, or absence when there is no focus item.</summary>
    public DirectoryEntry? FocusEntry { get; }

    /// <summary>Gets the status the pane shows.</summary>
    public PaneStatus Status { get; }

    /// <summary>Gets the canonical text of the listed or targeted location, or empty text.</summary>
    public string AddressText { get; }
}
