using System;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Represents the visible, focused name editor together with the text it starts from: empty for a
/// new directory, the focus item's provider-reported name for a rename.
/// </summary>
public sealed record ActiveNameEntry : NameEntryPresentation
{
    internal ActiveNameEntry(string initialText)
    {
        ArgumentNullException.ThrowIfNull(initialText);
        InitialText = initialText;
    }

    /// <summary>Gets the text the host assigns to the editor when it becomes visible.</summary>
    public string InitialText { get; }
}
